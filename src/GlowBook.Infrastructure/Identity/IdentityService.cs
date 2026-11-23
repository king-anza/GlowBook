using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GlowBook.Application.Identity;
using GlowBook.Infrastructure.Persistence;
using GlowBook.Domain.Operations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace GlowBook.Infrastructure.Identity;

internal sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    GlowBookDbContext dbContext,
    IIdentityEmailSender emailSender,
    IOptions<JwtOptions> options,
    ILogger<IdentityService> logger) : IIdentityService
{
    private readonly JwtOptions _options = options.Value;

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, string role, CancellationToken cancellationToken)
    {
        if (!request.AcceptTerms) return Failure("terms_required", "Terms must be accepted.");
        if (!request.ConfirmMinimumAge) return Failure("minimum_age_required", "The minimum age must be confirmed.");
        var now = DateTime.UtcNow;
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = request.Email.Trim(), Email = request.Email.Trim(), DisplayName = request.DisplayName.Trim(), CreatedAtUtc = now, TermsAcceptedAtUtc = now, PrivacyNoticeAcceptedAtUtc = now };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded) return Failure("registration_failed", "Registration could not be completed.");
        await userManager.AddToRoleAsync(user, role);
        dbContext.AuditLogs.Add(Audit(user.Id, "AccountRegistered")); await dbContext.SaveChangesAsync(cancellationToken);
        try { await emailSender.SendVerificationAsync(user.Email, user.Id, await userManager.GenerateEmailConfirmationTokenAsync(user), cancellationToken); }
        catch (Exception exception) { logger.LogError(exception, "Verification email delivery failed for user {UserId}", user.Id); }
        return new AuthResult(true);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password) || !user.EmailConfirmed || user.AccountStatus != "Active") return Failure("invalid_credentials", "Invalid email or password.");
        user.LastLoginAtUtc = DateTime.UtcNow;
        var tokens = await IssueTokensAsync(user, null, cancellationToken); dbContext.AuditLogs.Add(Audit(user.Id, "LoginSucceeded")); await dbContext.SaveChangesAsync(cancellationToken); return new AuthResult(true, Tokens: tokens);
    }

    public async Task<AuthResult> RefreshAsync(string rawToken, CancellationToken cancellationToken)
    {
        var hash = Hash(rawToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({hash}))", cancellationToken);
        var current = await dbContext.RefreshTokens.Include(x => x.User).SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (current is null) return Failure("invalid_refresh_token", "Refresh token is invalid.");
        if (!current.User.EmailConfirmed || current.User.AccountStatus != "Active") return Failure("invalid_refresh_token", "Refresh token is invalid.");
        if (current.RevokedAtUtc is not null)
        {
            await RevokeFamilyAsync(current.FamilyId, "Token reuse detected", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Failure("invalid_refresh_token", "Refresh token is invalid.");
        }
        if (current.ExpiresAtUtc <= DateTime.UtcNow) return Failure("invalid_refresh_token", "Refresh token is expired.");
        current.RevokedAtUtc = DateTime.UtcNow;
        current.RevocationReason = "Rotated";
        var tokens = await IssueTokensAsync(current.User, current, cancellationToken);
        dbContext.AuditLogs.Add(Audit(current.UserId, "RefreshTokenRotated")); await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new AuthResult(true, Tokens: tokens);
    }

    public async Task LogoutAsync(string rawToken, CancellationToken cancellationToken)
    {
        var token = await dbContext.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == Hash(rawToken), cancellationToken);
        if (token is null || token.RevokedAtUtc is not null) return;
        token.RevokedAtUtc = DateTime.UtcNow;
        token.RevocationReason = "Logout";
        dbContext.AuditLogs.Add(Audit(token.UserId, "Logout"));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null || !(await userManager.ConfirmEmailAsync(user, request.Token)).Succeeded) return false;
        dbContext.AuditLogs.Add(Audit(user.Id, "EmailVerified")); await dbContext.SaveChangesAsync(cancellationToken); return true;
    }

    public async Task RequestEmailVerificationAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is not null && !user.EmailConfirmed)
            await emailSender.SendVerificationAsync(user.Email!, user.Id, await userManager.GenerateEmailConfirmationTokenAsync(user), cancellationToken);
    }

    public async Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is not null && user.EmailConfirmed)
            await emailSender.SendPasswordResetAsync(user.Email!, await userManager.GeneratePasswordResetTokenAsync(user), cancellationToken);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !(await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword)).Succeeded) return false;
        dbContext.AuditLogs.Add(Audit(user.Id, "PasswordReset")); await dbContext.SaveChangesAsync(cancellationToken); return true;
    }

    public async Task<CurrentUser?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : new CurrentUser(user.Id, user.Email!, user.DisplayName, (await userManager.GetRolesAsync(user)).ToArray());
    }

    private async Task<AuthTokens> IssueTokensAsync(ApplicationUser user, RefreshToken? replaced, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("security_stamp", user.SecurityStamp ?? string.Empty)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, expires: expires, signingCredentials: credentials);
        var rawRefresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refresh = new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id, TokenHash = Hash(rawRefresh), FamilyId = replaced?.FamilyId ?? Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow, ExpiresAtUtc = DateTime.UtcNow.AddDays(_options.RefreshTokenDays) };
        if (replaced is not null) replaced.ReplacedByTokenId = refresh.Id;
        dbContext.RefreshTokens.Add(refresh);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new AuthTokens(new JwtSecurityTokenHandler().WriteToken(jwt), expires, rawRefresh);
    }

    private async Task RevokeFamilyAsync(Guid familyId, string reason, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens.Where(x => x.FamilyId == familyId && x.RevokedAtUtc == null).ToListAsync(cancellationToken);
        foreach (var token in tokens) { token.RevokedAtUtc = DateTime.UtcNow; token.RevocationReason = reason; }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static AuditLog Audit(Guid userId, string action) => new() { Id = Guid.NewGuid(), ActorUserId = userId, Action = action, SubjectType = "User", SubjectId = userId, OccurredAtUtc = DateTime.UtcNow };
    private static AuthResult Failure(string code, params string[] errors) => new(false, code, errors);
}
