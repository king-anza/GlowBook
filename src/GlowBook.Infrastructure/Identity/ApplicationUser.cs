using Microsoft.AspNetCore.Identity;

namespace GlowBook.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public string AccountStatus { get; set; } = "Active";
    public string TermsVersion { get; set; } = "2026-11-07";
    public DateTime TermsAcceptedAtUtc { get; set; }
    public string PrivacyNoticeVersion { get; set; } = "2026-11-07";
    public DateTime PrivacyNoticeAcceptedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public bool MarketingConsent { get; set; }
    public DateTime? MarketingConsentUpdatedAtUtc { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}

public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public Guid FamilyId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevocationReason { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public ApplicationUser User { get; set; } = null!;
}
