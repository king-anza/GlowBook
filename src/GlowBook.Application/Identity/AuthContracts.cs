using System.ComponentModel.DataAnnotations;

namespace GlowBook.Application.Identity;

public sealed record RegisterRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(10)] string Password,
    [property: Required, MaxLength(100)] string DisplayName,
    [property: Required] bool AcceptTerms,
    [property: Required] bool ConfirmMinimumAge);

public sealed record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public sealed record RefreshRequest([property: Required] string RefreshToken);
public sealed record TokenRequest([property: Required] string Token);
public sealed record VerifyEmailRequest([property: Required] Guid UserId, [property: Required] string Token);
public sealed record EmailRequest([property: Required, EmailAddress] string Email);
public sealed record ResetPasswordRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Token,
    [property: Required, MinLength(10)] string NewPassword);

public sealed record AuthTokens(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken);
public sealed record CurrentUser(Guid Id, string Email, string DisplayName, IReadOnlyCollection<string> Roles);
public sealed record AuthResult(bool Succeeded, string? ErrorCode = null, IReadOnlyCollection<string>? Errors = null, AuthTokens? Tokens = null);

public interface IIdentityService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, string role, CancellationToken cancellationToken);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
    Task<bool> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken);
    Task RequestEmailVerificationAsync(string email, CancellationToken cancellationToken);
    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
    Task<CurrentUser?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}

public interface IIdentityEmailSender
{
    Task SendVerificationAsync(string email, Guid userId, string token, CancellationToken cancellationToken);
    Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken);
}

public interface IEmailClient
{
    Task<bool> SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken);
}
