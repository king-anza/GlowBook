namespace GlowBook.Domain.Spaces;

public enum WorkspaceType { StylingChair, BarberChair, NailStation, MakeupStation, TreatmentRoom, MassageRoom, TattooStation, EntireStudio, Other }
public enum OpportunityType { ChairRental, StaffVacancy }
public enum OpportunityStatus { Draft, Published, Filled, Paused, Closed }
public enum RatePeriod { Hour, Day, Week, Month, RevenueShare, Negotiable }
public enum ApplicationStatus { Pending, Accepted, Declined, Withdrawn }

public sealed class WorkspaceOpportunity
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public WorkspaceType WorkspaceType { get; set; }
    public OpportunityType OpportunityType { get; set; }
    public OpportunityStatus Status { get; set; } = OpportunityStatus.Draft;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequiredSkills { get; set; } = string.Empty;
    public string IncludedAmenities { get; set; } = string.Empty;
    public string HouseRules { get; set; } = string.Empty;
    public int SpacesAvailable { get; set; } = 1;
    public decimal? RateAmount { get; set; }
    public RatePeriod RatePeriod { get; set; } = RatePeriod.Negotiable;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool Recurring { get; set; }
    public bool BringOwnClients { get; set; }
    public bool ApprovalRequired { get; set; } = true;
    public string? ImageUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public GlowBook.Domain.Professionals.Business Business { get; set; } = null!;
    public ICollection<WorkspaceApplication> Applications { get; set; } = [];
}

public sealed class WorkspaceApplication
{
    public Guid Id { get; set; }
    public Guid OpportunityId { get; set; }
    public Guid ApplicantUserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PortfolioUrl { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public WorkspaceOpportunity Opportunity { get; set; } = null!;
}
