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
    [property: Url] string? ImageUrl);

public sealed record OpportunityDto(Guid Id, Guid BusinessId, string BusinessName, string City, string Province,
    WorkspaceType WorkspaceType, OpportunityType OpportunityType, OpportunityStatus Status, string Title,
    string Description, string RequiredSkills, string IncludedAmenities, string HouseRules, int SpacesAvailable,
    decimal? RateAmount, RatePeriod RatePeriod, DateOnly StartDate, DateOnly? EndDate, bool Recurring,
    bool BringOwnClients, bool ApprovalRequired, string? ImageUrl, int ApplicationCount, DateTime CreatedAtUtc);

public sealed record ApplicationRequest([property: Required, MaxLength(1500)] string Message, [property: Url, MaxLength(2048)] string? PortfolioUrl);
public sealed record ApplicationDto(Guid Id, Guid OpportunityId, string OpportunityTitle, Guid ApplicantUserId,
    string ApplicantName, string ApplicantEmail, string Message, string? PortfolioUrl, ApplicationStatus Status, DateTime CreatedAtUtc);

public interface ISpaceService
{
    Task<IReadOnlyCollection<OpportunityDto>> DiscoverAsync(string? city, OpportunityType? type, WorkspaceType? workspaceType, CancellationToken ct);
    Task<IReadOnlyCollection<OpportunityDto>> OwnerOpportunitiesAsync(Guid ownerId, CancellationToken ct);
    Task<OpportunityDto?> SaveAsync(Guid ownerId, Guid? id, OpportunityRequest request, CancellationToken ct);
    Task<bool> SetStatusAsync(Guid ownerId, Guid id, OpportunityStatus status, CancellationToken ct);
    Task<ApplicationDto?> ApplyAsync(Guid userId, Guid opportunityId, ApplicationRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<ApplicationDto>> OwnerApplicationsAsync(Guid ownerId, CancellationToken ct);
    Task<IReadOnlyCollection<ApplicationDto>> ApplicantApplicationsAsync(Guid userId, CancellationToken ct);
    Task<bool> ReviewApplicationAsync(Guid ownerId, Guid applicationId, ApplicationStatus status, CancellationToken ct);
}
