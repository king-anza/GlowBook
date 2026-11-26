using System.Text.Json.Serialization;

namespace GlowBook.Domain.Spaces;

[JsonConverter(typeof(JsonStringEnumConverter<WorkspaceType>))]
public enum WorkspaceType { StylingChair, BarberChair, NailStation, MakeupStation, TreatmentRoom, MassageRoom, TattooStation, EntireStudio, Other }
[JsonConverter(typeof(JsonStringEnumConverter<OpportunityType>))]
public enum OpportunityType { ChairRental, StaffVacancy }
[JsonConverter(typeof(JsonStringEnumConverter<OpportunityStatus>))]
public enum OpportunityStatus { Draft, Published, Filled, Paused, Closed }
[JsonConverter(typeof(JsonStringEnumConverter<RatePeriod>))]
public enum RatePeriod { Hour, Day, Week, Month, RevenueShare, Negotiable }
[JsonConverter(typeof(JsonStringEnumConverter<ApplicationStatus>))]
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
    public string[] ImageUrls { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public GlowBook.Domain.Professionals.Business Business { get; set; } = null!;
    public ICollection<WorkspaceApplication> Applications { get; set; } = [];
    public ICollection<SavedWorkspaceOpportunity> Saves { get; set; } = [];
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

public sealed class SavedWorkspaceOpportunity
{
    public Guid UserId { get; set; }
    public Guid OpportunityId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public WorkspaceOpportunity Opportunity { get; set; } = null!;
}

[JsonConverter(typeof(JsonStringEnumConverter<OpportunityReportReason>))]
public enum OpportunityReportReason { Misleading, SafetyConcern, Discrimination, Spam, Other }

public sealed class WorkspaceOpportunityReport
{
    public Guid Id { get; set; }
    public Guid OpportunityId { get; set; }
    public Guid ReporterUserId { get; set; }
    public OpportunityReportReason Reason { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public WorkspaceOpportunity Opportunity { get; set; } = null!;
}
