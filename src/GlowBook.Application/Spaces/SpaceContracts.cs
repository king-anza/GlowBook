using System.ComponentModel.DataAnnotations;
using GlowBook.Domain.Spaces;

namespace GlowBook.Application.Spaces;

public sealed record OpportunityRequest(
    WorkspaceType WorkspaceType,
    OpportunityType OpportunityType,
    [property: Required, MaxLength(150)] string Title,
    [property: Required, MaxLength(2000)] string Description,
    [property: MaxLength(1000)] string RequiredSkills,
    [property: MaxLength(1000)] string IncludedAmenities,
    [property: MaxLength(1500)] string HouseRules,
    [property: Range(1, 50)] int SpacesAvailable,
    [property: Range(0, 999999)] decimal? RateAmount,
    RatePeriod RatePeriod,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool Recurring,
    bool BringOwnClients,
    bool ApprovalRequired,
    [property: Url] string? ImageUrl,
    [property: MaxLength(5)] string[]? ImageUrls);

public sealed record OpportunityDto(Guid Id, Guid BusinessId, string BusinessName, string City, string Province,
    WorkspaceType WorkspaceType, OpportunityType OpportunityType, OpportunityStatus Status, string Title,
    string Description, string RequiredSkills, string IncludedAmenities, string HouseRules, int SpacesAvailable,
    decimal? RateAmount, RatePeriod RatePeriod, DateOnly StartDate, DateOnly? EndDate, bool Recurring,
    bool BringOwnClients, bool ApprovalRequired, string? ImageUrl, IReadOnlyCollection<string> ImageUrls,
    int ApplicationCount, DateTime CreatedAtUtc, bool IsVerifiedParty, bool IsDemo, bool IsExpired, bool IsUrgent,
    double? DistanceKm, bool IsSaved);

public sealed record ApplicationRequest([property: Required, MaxLength(1500)] string Message, [property: Url, MaxLength(2048)] string? PortfolioUrl);
public sealed record ApplicationDto(Guid Id, Guid OpportunityId, string OpportunityTitle, Guid ApplicantUserId,
    string ApplicantName, string ApplicantEmail, string Message, string? PortfolioUrl, ApplicationStatus Status, DateTime CreatedAtUtc);
public sealed record OpportunityReportRequest(OpportunityReportReason Reason, [property: MaxLength(1000)] string? Details);
public sealed record OpportunityReportDto(Guid Id, Guid OpportunityId, string OpportunityTitle, string BusinessName,
    OpportunityReportReason Reason, string Details, DateTime CreatedAtUtc);

public interface ISpaceService
{
    Task<IReadOnlyCollection<OpportunityDto>> DiscoverAsync(string? city, OpportunityType? type, WorkspaceType? workspaceType,
        double? latitude, double? longitude, Guid? userId, CancellationToken ct);
    Task<IReadOnlyCollection<OpportunityDto>> OwnerOpportunitiesAsync(Guid ownerId, CancellationToken ct);
    Task<OpportunityDto?> SaveAsync(Guid ownerId, Guid? id, OpportunityRequest request, CancellationToken ct);
    Task<bool> SetStatusAsync(Guid ownerId, Guid id, OpportunityStatus status, CancellationToken ct);
    Task<ApplicationDto?> ApplyAsync(Guid userId, Guid opportunityId, ApplicationRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<ApplicationDto>> OwnerApplicationsAsync(Guid ownerId, CancellationToken ct);
    Task<IReadOnlyCollection<ApplicationDto>> ApplicantApplicationsAsync(Guid userId, CancellationToken ct);
    Task<bool> ReviewApplicationAsync(Guid ownerId, Guid applicationId, ApplicationStatus status, CancellationToken ct);
    Task<bool> SaveOpportunityAsync(Guid userId, Guid opportunityId, CancellationToken ct);
    Task<bool> RemoveSavedOpportunityAsync(Guid userId, Guid opportunityId, CancellationToken ct);
    Task<IReadOnlyCollection<OpportunityDto>> SavedAsync(Guid userId, CancellationToken ct);
    Task<bool> ReportAsync(Guid userId, Guid opportunityId, OpportunityReportRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<OpportunityReportDto>> ReportsAsync(CancellationToken ct);
}
