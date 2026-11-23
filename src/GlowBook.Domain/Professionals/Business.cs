namespace GlowBook.Domain.Professionals;

public enum BusinessApprovalStatus { Draft, PendingReview, Approved, Rejected, Suspended }
public enum PortfolioImageStatus { Active, Hidden }

public sealed class Business
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string TradingName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CancellationPolicy { get; set; } = string.Empty;
    public string? PublicEmail { get; set; }
    public string? PublicPhone { get; set; }
    public BusinessApprovalStatus ApprovalStatus { get; set; } = BusinessApprovalStatus.Draft;
    public string? ReviewReason { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public ICollection<BusinessLocation> Locations { get; set; } = [];
    public ICollection<StaffMember> Staff { get; set; } = [];
    public ICollection<PortfolioImage> PortfolioImages { get; set; } = [];
}

public sealed class PortfolioImage
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public PortfolioImageStatus Status { get; set; } = PortfolioImageStatus.Active;
    public DateTime CreatedAtUtc { get; set; }
    public Business Business { get; set; } = null!;
}

public sealed class BusinessLocation
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = "Primary location";
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string Suburb { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "Africa/Johannesburg";
    public bool IsPrimary { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public Business Business { get; set; } = null!;
}

public sealed class StaffMember
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? LinkedUserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Biography { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool AcceptingBookings { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Business Business { get; set; } = null!;
}
