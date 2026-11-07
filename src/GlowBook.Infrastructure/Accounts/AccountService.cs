using GlowBook.Application.Accounts;
using GlowBook.Application.Identity;
using GlowBook.Domain.Operations;
using GlowBook.Infrastructure.Identity;
using GlowBook.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GlowBook.Domain.Professionals;

namespace GlowBook.Infrastructure.Accounts;

internal sealed class AccountService(GlowBookDbContext db, UserManager<ApplicationUser> users, IIdentityEmailSender emailSender, ILogger<AccountService> logger) : IAccountService
{
    public async Task<AccountDataDto?> ExportAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, ct); if (user is null) return null;
        var bookings = await db.Bookings.AsNoTracking().Where(x => x.CustomerUserId == userId).OrderByDescending(x => x.StartUtc).Select(x => new AccountBookingDataDto(x.Id, x.Business.TradingName, x.ServiceNameSnapshot, x.PriceSnapshot, x.DurationMinutesSnapshot, x.StartUtc, x.EndUtc, x.Status.ToString(), x.CustomerNotes)).ToListAsync(ct);
        var reviews = await db.Reviews.AsNoTracking().Where(x => x.CustomerUserId == userId).OrderByDescending(x => x.CreatedAtUtc).Select(x => new AccountReviewDataDto(x.Id, x.BookingId, x.Rating, x.Comment, x.ModerationStatus.ToString(), x.CreatedAtUtc)).ToListAsync(ct);
        var business = await db.Businesses.AsNoTracking().Where(x => x.OwnerUserId == userId).Select(x => new AccountBusinessDataDto(x.Id, x.TradingName, x.Description, x.PublicEmail, x.PublicPhone, x.ApprovalStatus.ToString(), x.Locations.Where(l => l.IsPrimary).Select(l => l.AddressLine1).Single(), x.Locations.Where(l => l.IsPrimary).Select(l => l.AddressLine2).Single(), x.Locations.Where(l => l.IsPrimary).Select(l => l.Suburb).Single(), x.Locations.Where(l => l.IsPrimary).Select(l => l.City).Single(), x.Locations.Where(l => l.IsPrimary).Select(l => l.Province).Single(), x.Locations.Where(l => l.IsPrimary).Select(l => l.PostalCode).Single())).SingleOrDefaultAsync(ct);
        return new(user.Id, user.Email ?? string.Empty, user.DisplayName, user.MarketingConsent, user.AccountStatus, user.CreatedAtUtc, user.TermsAcceptedAtUtc, user.TermsVersion, user.PrivacyNoticeAcceptedAtUtc, user.PrivacyNoticeVersion, bookings, reviews, business);
    }

    public async Task<AccountDataDto?> UpdateAsync(Guid userId, UpdateAccountRequest request, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString()); if (user is null || user.AccountStatus != "Active") return null;
        var emailChanged = !string.Equals(user.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase);
        if (emailChanged)
        {
            var emailResult = await users.SetEmailAsync(user, request.Email.Trim()); if (!emailResult.Succeeded) return null;
            var userNameResult = await users.SetUserNameAsync(user, request.Email.Trim()); if (!userNameResult.Succeeded) return null;
            var activeTokens = await db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAtUtc == null).ToListAsync(ct);
            foreach (var token in activeTokens) { token.RevokedAtUtc = DateTime.UtcNow; token.RevocationReason = "Email changed"; }
        }
        user.DisplayName = request.DisplayName.Trim(); user.MarketingConsent = request.MarketingConsent; user.MarketingConsentUpdatedAtUtc = DateTime.UtcNow;
        var result = await users.UpdateAsync(user); if (!result.Succeeded) return null;
        db.AuditLogs.Add(Audit(userId, "AccountUpdated")); await db.SaveChangesAsync(ct);
        if (emailChanged)
            try { await emailSender.SendVerificationAsync(user.Email!, user.Id, await users.GenerateEmailConfirmationTokenAsync(user), ct); }
            catch (Exception exception) { logger.LogError(exception, "Verification email delivery failed after account email change for user {UserId}", user.Id); }
        return await ExportAsync(userId, ct);
    }

    public async Task<bool> AnonymiseAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString()); if (user is null || user.AccountStatus == "Anonymised") return false;
        var suffix = user.Id.ToString("N"); user.DisplayName = "Deleted user"; user.Email = $"deleted-{suffix}@invalid.local"; user.NormalizedEmail = user.Email.ToUpperInvariant(); user.UserName = user.Email; user.NormalizedUserName = user.NormalizedEmail; user.PhoneNumber = null; user.PasswordHash = null; user.SecurityStamp = Guid.NewGuid().ToString(); user.AccountStatus = "Anonymised"; user.MarketingConsent = false; user.MarketingConsentUpdatedAtUtc = DateTime.UtcNow;
        var tokens = await db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAtUtc == null).ToListAsync(ct); foreach (var token in tokens) { token.RevokedAtUtc = DateTime.UtcNow; token.RevocationReason = "Account anonymised"; }
        var notifications = await db.Notifications.Where(x => x.RecipientUserId == userId && x.Status == NotificationStatus.Pending).ToListAsync(ct); foreach (var notification in notifications) { notification.Status = NotificationStatus.Failed; notification.ProcessedAtUtc = DateTime.UtcNow; }
        var business = await db.Businesses.SingleOrDefaultAsync(x => x.OwnerUserId == userId, ct); if (business is not null) { business.ApprovalStatus = BusinessApprovalStatus.Suspended; business.PublicEmail = null; business.PublicPhone = null; business.ReviewReason = "Owner account anonymised"; business.UpdatedAtUtc = DateTime.UtcNow; }
        var result = await users.UpdateAsync(user); if (!result.Succeeded) return false;
        db.AuditLogs.Add(Audit(userId, "AccountAnonymised")); await db.SaveChangesAsync(ct); return true;
    }

    private static AuditLog Audit(Guid actorId, string action) => new() { Id = Guid.NewGuid(), ActorUserId = actorId, Action = action, SubjectType = "User", SubjectId = actorId, OccurredAtUtc = DateTime.UtcNow };
}
