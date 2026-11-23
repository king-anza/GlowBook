using System.ComponentModel.DataAnnotations;

namespace GlowBook.Application.Accounts;

public sealed record UpdateAccountRequest([property: Required, EmailAddress] string Email, [property: Required, MaxLength(100)] string DisplayName, bool MarketingConsent);
public sealed record AccountBookingDataDto(Guid Id, string BusinessName, string ServiceName, decimal Price, int DurationMinutes, DateTime StartUtc, DateTime EndUtc, string Status, string? CustomerNotes);
public sealed record AccountReviewDataDto(Guid Id, Guid BookingId, int Rating, string? Comment, string ModerationStatus, DateTime CreatedAtUtc);
public sealed record AccountBusinessDataDto(Guid Id, string TradingName, string Description, string? PublicEmail, string? PublicPhone, string ApprovalStatus, string AddressLine1, string? AddressLine2, string Suburb, string City, string Province, string PostalCode);
public sealed record AccountDataDto(Guid Id, string Email, string DisplayName, bool MarketingConsent, string AccountStatus, DateTime CreatedAtUtc, DateTime TermsAcceptedAtUtc, string TermsVersion, DateTime PrivacyNoticeAcceptedAtUtc, string PrivacyNoticeVersion, IReadOnlyCollection<AccountBookingDataDto> Bookings, IReadOnlyCollection<AccountReviewDataDto> Reviews, AccountBusinessDataDto? Business);

public interface IAccountService
{
    Task<AccountDataDto?> ExportAsync(Guid userId, CancellationToken ct);
    Task<AccountDataDto?> UpdateAsync(Guid userId, UpdateAccountRequest request, CancellationToken ct);
    Task<bool> AnonymiseAsync(Guid userId, CancellationToken ct);
}
