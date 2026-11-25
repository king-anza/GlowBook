using GlowBook.Application.Spaces;
using GlowBook.Domain.Spaces;
using GlowBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GlowBook.Infrastructure.Spaces;

internal sealed class SpaceService(GlowBookDbContext db) : ISpaceService
{
    public async Task<IReadOnlyCollection<OpportunityDto>> DiscoverAsync(string? city, OpportunityType? type, WorkspaceType? workspaceType, CancellationToken ct)
    {
        var query = db.WorkspaceOpportunities.AsNoTracking().Where(x => x.Status == OpportunityStatus.Published && x.Business.ApprovalStatus == GlowBook.Domain.Professionals.BusinessApprovalStatus.Approved);
        if (!string.IsNullOrWhiteSpace(city)) query = query.Where(x => x.Business.Locations.Any(l => l.IsPrimary && (l.City.ToLower().Contains(city.ToLower()) || l.Suburb.ToLower().Contains(city.ToLower()))));
        if (type is not null) query = query.Where(x => x.OpportunityType == type);
        if (workspaceType is not null) query = query.Where(x => x.WorkspaceType == workspaceType);
        return await query.OrderByDescending(x => x.CreatedAtUtc).Select(OpportunityProjection).ToArrayAsync(ct);
    }

    public async Task<IReadOnlyCollection<OpportunityDto>> OwnerOpportunitiesAsync(Guid ownerId, CancellationToken ct) =>
        await db.WorkspaceOpportunities.AsNoTracking().Where(x => x.Business.OwnerUserId == ownerId).OrderByDescending(x => x.CreatedAtUtc).Select(OpportunityProjection).ToArrayAsync(ct);

    public async Task<OpportunityDto?> SaveAsync(Guid ownerId, Guid? id, OpportunityRequest r, CancellationToken ct)
    {
        var business = await db.Businesses.Include(x => x.Locations).SingleOrDefaultAsync(x => x.OwnerUserId == ownerId, ct);
        if (business is null || r.EndDate < r.StartDate) return null;
        var item = id is null ? new WorkspaceOpportunity { Id = Guid.NewGuid(), BusinessId = business.Id, CreatedAtUtc = DateTime.UtcNow } :
            await db.WorkspaceOpportunities.Include(x => x.Business).SingleOrDefaultAsync(x => x.Id == id && x.Business.OwnerUserId == ownerId, ct);
        if (item is null) return null;
        item.WorkspaceType = r.WorkspaceType; item.OpportunityType = r.OpportunityType; item.Title = r.Title.Trim();
        item.Description = r.Description.Trim(); item.RequiredSkills = r.RequiredSkills.Trim(); item.IncludedAmenities = r.IncludedAmenities.Trim();
        item.HouseRules = r.HouseRules.Trim(); item.SpacesAvailable = r.SpacesAvailable; item.RateAmount = r.RateAmount;
        item.RatePeriod = r.RatePeriod; item.StartDate = r.StartDate; item.EndDate = r.EndDate; item.Recurring = r.Recurring;
        item.BringOwnClients = r.BringOwnClients; item.ApprovalRequired = r.ApprovalRequired; item.ImageUrl = r.ImageUrl; item.UpdatedAtUtc = DateTime.UtcNow;
        if (id is null) db.WorkspaceOpportunities.Add(item);
        await db.SaveChangesAsync(ct);
        return await db.WorkspaceOpportunities.AsNoTracking().Where(x => x.Id == item.Id).Select(OpportunityProjection).SingleAsync(ct);
    }

    public async Task<bool> SetStatusAsync(Guid ownerId, Guid id, OpportunityStatus status, CancellationToken ct)
    {
        var item = await db.WorkspaceOpportunities.Include(x => x.Business).SingleOrDefaultAsync(x => x.Id == id && x.Business.OwnerUserId == ownerId, ct);
        if (item is null || status == OpportunityStatus.Published && item.Business.ApprovalStatus != GlowBook.Domain.Professionals.BusinessApprovalStatus.Approved) return false;
        item.Status = status; item.UpdatedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct); return true;
    }

    public async Task<ApplicationDto?> ApplyAsync(Guid userId, Guid opportunityId, ApplicationRequest r, CancellationToken ct)
    {
        var opportunity = await db.WorkspaceOpportunities.Include(x => x.Business).SingleOrDefaultAsync(x => x.Id == opportunityId && x.Status == OpportunityStatus.Published, ct);
        if (opportunity?.Business.OwnerUserId == userId) return null;
        if (opportunity is null || await db.WorkspaceApplications.AnyAsync(x => x.OpportunityId == opportunityId && x.ApplicantUserId == userId, ct)) return null;
        var item = new WorkspaceApplication { Id = Guid.NewGuid(), OpportunityId = opportunityId, ApplicantUserId = userId, Message = r.Message.Trim(), PortfolioUrl = r.PortfolioUrl?.Trim() ?? string.Empty, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        db.WorkspaceApplications.Add(item); await db.SaveChangesAsync(ct);
        return await ApplicationQuery().SingleAsync(x => x.Id == item.Id, ct);
    }

    public async Task<IReadOnlyCollection<ApplicationDto>> OwnerApplicationsAsync(Guid ownerId, CancellationToken ct) => await ApplicationQuery(ownerId, null).OrderByDescending(x => x.CreatedAtUtc).ToArrayAsync(ct);
    public async Task<IReadOnlyCollection<ApplicationDto>> ApplicantApplicationsAsync(Guid userId, CancellationToken ct) => await ApplicationQuery(null, userId).OrderByDescending(x => x.CreatedAtUtc).ToArrayAsync(ct);
    public async Task<bool> ReviewApplicationAsync(Guid ownerId, Guid applicationId, ApplicationStatus status, CancellationToken ct)
    {
        if (status is not (ApplicationStatus.Accepted or ApplicationStatus.Declined)) return false;
        var item = await db.WorkspaceApplications.Include(x => x.Opportunity).ThenInclude(x => x.Business).SingleOrDefaultAsync(x => x.Id == applicationId && x.Opportunity.Business.OwnerUserId == ownerId, ct);
        if (item is null || item.Status != ApplicationStatus.Pending) return false;
        item.Status = status; item.UpdatedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct); return true;
    }

    private IQueryable<ApplicationDto> ApplicationQuery(Guid? ownerId = null, Guid? applicantId = null) =>
        from application in db.WorkspaceApplications.AsNoTracking()
        join user in db.Users.AsNoTracking() on application.ApplicantUserId equals user.Id
        where (ownerId == null || application.Opportunity.Business.OwnerUserId == ownerId) &&
              (applicantId == null || application.ApplicantUserId == applicantId)
        select new ApplicationDto(application.Id, application.OpportunityId, application.Opportunity.Title,
            application.ApplicantUserId, user.DisplayName, user.Email ?? string.Empty, application.Message,
            application.PortfolioUrl == string.Empty ? null : application.PortfolioUrl, application.Status, application.CreatedAtUtc);

    private static readonly Expression<Func<WorkspaceOpportunity, OpportunityDto>> OpportunityProjection = x => new(x.Id, x.BusinessId, x.Business.TradingName,
        x.Business.Locations.Where(l => l.IsPrimary).Select(l => l.City).FirstOrDefault() ?? string.Empty,
        x.Business.Locations.Where(l => l.IsPrimary).Select(l => l.Province).FirstOrDefault() ?? string.Empty,
        x.WorkspaceType, x.OpportunityType, x.Status, x.Title, x.Description, x.RequiredSkills, x.IncludedAmenities,
        x.HouseRules, x.SpacesAvailable, x.RateAmount, x.RatePeriod, x.StartDate, x.EndDate, x.Recurring,
        x.BringOwnClients, x.ApprovalRequired, x.ImageUrl, x.Applications.Count, x.CreatedAtUtc);
}
