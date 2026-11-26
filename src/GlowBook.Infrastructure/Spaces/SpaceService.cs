using GlowBook.Application.Spaces;
using GlowBook.Domain.Spaces;
using GlowBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GlowBook.Infrastructure.Spaces;

internal sealed class SpaceService(GlowBookDbContext db) : ISpaceService
{
    public async Task<IReadOnlyCollection<OpportunityDto>> DiscoverAsync(string? city, OpportunityType? type, WorkspaceType? workspaceType,
        double? latitude, double? longitude, Guid? userId, CancellationToken ct)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180 || (latitude is null) != (longitude is null))
            latitude = longitude = null;
        var query = db.WorkspaceOpportunities.AsNoTracking().Where(x => x.Status == OpportunityStatus.Published && x.Business.ApprovalStatus == GlowBook.Domain.Professionals.BusinessApprovalStatus.Approved);
        if (!string.IsNullOrWhiteSpace(city)) query = query.Where(x => x.Business.Locations.Any(l => l.IsPrimary && (l.City.ToLower().Contains(city.ToLower()) || l.Suburb.ToLower().Contains(city.ToLower()))));
        if (type is not null) query = query.Where(x => x.OpportunityType == type);
        if (workspaceType is not null) query = query.Where(x => x.WorkspaceType == workspaceType);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).Select(OpportunityProjection).ToArrayAsync(ct);
        return await EnrichAsync(items, latitude, longitude, userId, ct);
    }

    public async Task<IReadOnlyCollection<OpportunityDto>> OwnerOpportunitiesAsync(Guid ownerId, CancellationToken ct) =>
        await db.WorkspaceOpportunities.AsNoTracking().Where(x => x.Business.OwnerUserId == ownerId).OrderByDescending(x => x.CreatedAtUtc).Select(OpportunityProjection).ToArrayAsync(ct);

    public async Task<OpportunityDto?> SaveAsync(Guid ownerId, Guid? id, OpportunityRequest r, CancellationToken ct)
    {
        var business = await db.Businesses.Include(x => x.Locations).SingleOrDefaultAsync(x => x.OwnerUserId == ownerId, ct);
        if (business is null || r.EndDate < r.StartDate || r.ImageUrl is not null && !IsHttpsUrl(r.ImageUrl)) return null;
        var item = id is null ? new WorkspaceOpportunity { Id = Guid.NewGuid(), BusinessId = business.Id, CreatedAtUtc = DateTime.UtcNow } :
            await db.WorkspaceOpportunities.Include(x => x.Business).SingleOrDefaultAsync(x => x.Id == id && x.Business.OwnerUserId == ownerId, ct);
        if (item is null) return null;
        item.WorkspaceType = r.WorkspaceType; item.OpportunityType = r.OpportunityType; item.Title = r.Title.Trim();
        item.Description = r.Description.Trim(); item.RequiredSkills = r.RequiredSkills.Trim(); item.IncludedAmenities = r.IncludedAmenities.Trim();
        item.HouseRules = r.HouseRules.Trim(); item.SpacesAvailable = r.SpacesAvailable; item.RateAmount = r.RateAmount;
        item.RatePeriod = r.RatePeriod; item.StartDate = r.StartDate; item.EndDate = r.EndDate; item.Recurring = r.Recurring;
        var images = (r.ImageUrls ?? []).Select(x => x.Trim()).Where(IsHttpsUrl).Distinct().Take(5).ToArray();
        if ((r.ImageUrls?.Length ?? 0) > 5 || (r.ImageUrls ?? []).Any(x => !IsHttpsUrl(x))) return null;
        item.BringOwnClients = r.BringOwnClients; item.ApprovalRequired = r.ApprovalRequired; item.ImageUrl = r.ImageUrl; item.ImageUrls = images; item.UpdatedAtUtc = DateTime.UtcNow;
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var opportunity = await db.WorkspaceOpportunities.Include(x => x.Business).SingleOrDefaultAsync(x => x.Id == opportunityId && x.Status == OpportunityStatus.Published && (x.EndDate == null || x.EndDate >= today), ct);
        if (opportunity?.Business.OwnerUserId == userId) return null;
        if (opportunity is null || await db.WorkspaceApplications.AnyAsync(x => x.OpportunityId == opportunityId && x.ApplicantUserId == userId, ct)) return null;
        var item = new WorkspaceApplication { Id = Guid.NewGuid(), OpportunityId = opportunityId, ApplicantUserId = userId, Message = r.Message.Trim(), PortfolioUrl = r.PortfolioUrl?.Trim() ?? string.Empty, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        db.WorkspaceApplications.Add(item);
        db.Notifications.Add(Notification(opportunity.Business.OwnerUserId, "ApplicationReceived", "New Glow Spaces application",
            $"A professional applied for {opportunity.Title}.", $"/professional/spaces?application={item.Id}", "WorkspaceApplication", item.Id));
        await db.SaveChangesAsync(ct);
        return await ApplicationQuery(applicationId: item.Id).SingleAsync(ct);
    }

    public async Task<IReadOnlyCollection<ApplicationDto>> OwnerApplicationsAsync(Guid ownerId, CancellationToken ct) => await ApplicationQuery(ownerId, null).OrderByDescending(x => x.CreatedAtUtc).ToArrayAsync(ct);
    public async Task<IReadOnlyCollection<ApplicationDto>> ApplicantApplicationsAsync(Guid userId, CancellationToken ct) => await ApplicationQuery(null, userId).OrderByDescending(x => x.CreatedAtUtc).ToArrayAsync(ct);
    public async Task<bool> ReviewApplicationAsync(Guid ownerId, Guid applicationId, ApplicationStatus status, CancellationToken ct)
    {
        if (status is not (ApplicationStatus.Accepted or ApplicationStatus.Declined)) return false;
        var item = await db.WorkspaceApplications.Include(x => x.Opportunity).ThenInclude(x => x.Business).SingleOrDefaultAsync(x => x.Id == applicationId && x.Opportunity.Business.OwnerUserId == ownerId, ct);
        if (item is null || item.Status != ApplicationStatus.Pending) return false;
        item.Status = status; item.UpdatedAtUtc = DateTime.UtcNow;
        db.Notifications.Add(Notification(item.ApplicantUserId, $"Application{status}", $"Application {status.ToString().ToLowerInvariant()}",
            $"Your application for {item.Opportunity.Title} was {status.ToString().ToLowerInvariant()}.", $"/spaces?opportunity={item.OpportunityId}", "WorkspaceApplication", item.Id));
        await db.SaveChangesAsync(ct); return true;
    }

    public async Task<bool> SaveOpportunityAsync(Guid userId, Guid opportunityId, CancellationToken ct)
    {
        if (!await db.WorkspaceOpportunities.AnyAsync(x => x.Id == opportunityId && x.Status == OpportunityStatus.Published, ct)) return false;
        if (!await db.SavedWorkspaceOpportunities.AnyAsync(x => x.UserId == userId && x.OpportunityId == opportunityId, ct))
        { db.SavedWorkspaceOpportunities.Add(new() { UserId = userId, OpportunityId = opportunityId, CreatedAtUtc = DateTime.UtcNow }); await db.SaveChangesAsync(ct); }
        return true;
    }

    public async Task<bool> RemoveSavedOpportunityAsync(Guid userId, Guid opportunityId, CancellationToken ct) =>
        await db.SavedWorkspaceOpportunities.Where(x => x.UserId == userId && x.OpportunityId == opportunityId).ExecuteDeleteAsync(ct) > 0;

    public async Task<IReadOnlyCollection<OpportunityDto>> SavedAsync(Guid userId, CancellationToken ct)
    {
        var items = await db.SavedWorkspaceOpportunities.AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc).Select(x => x.Opportunity).Select(OpportunityProjection).ToArrayAsync(ct);
        return await EnrichAsync(items, null, null, userId, ct);
    }

    public async Task<bool> ReportAsync(Guid userId, Guid opportunityId, OpportunityReportRequest request, CancellationToken ct)
    {
        if (!await db.WorkspaceOpportunities.AnyAsync(x => x.Id == opportunityId && x.Status == OpportunityStatus.Published && x.Business.OwnerUserId != userId, ct) ||
            await db.WorkspaceOpportunityReports.AnyAsync(x => x.OpportunityId == opportunityId && x.ReporterUserId == userId, ct)) return false;
        db.WorkspaceOpportunityReports.Add(new() { Id = Guid.NewGuid(), OpportunityId = opportunityId, ReporterUserId = userId,
            Reason = request.Reason, Details = request.Details?.Trim() ?? string.Empty, CreatedAtUtc = DateTime.UtcNow });
        db.AuditLogs.Add(new() { Id = Guid.NewGuid(), ActorUserId = userId, Action = "WorkspaceOpportunityReported",
            SubjectType = "WorkspaceOpportunity", SubjectId = opportunityId, Details = request.Reason.ToString(), OccurredAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(ct); return true;
    }

    public async Task<IReadOnlyCollection<OpportunityReportDto>> ReportsAsync(CancellationToken ct) =>
        await db.WorkspaceOpportunityReports.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(100)
            .Select(x => new OpportunityReportDto(x.Id, x.OpportunityId, x.Opportunity.Title, x.Opportunity.Business.TradingName,
                x.Reason, x.Details, x.CreatedAtUtc)).ToArrayAsync(ct);

    private IQueryable<ApplicationDto> ApplicationQuery(Guid? ownerId = null, Guid? applicantId = null, Guid? applicationId = null) =>
        from application in db.WorkspaceApplications.AsNoTracking()
        join user in db.Users.AsNoTracking() on application.ApplicantUserId equals user.Id
        where (ownerId == null || application.Opportunity.Business.OwnerUserId == ownerId) &&
              (applicantId == null || application.ApplicantUserId == applicantId) &&
              (applicationId == null || application.Id == applicationId)
        select new ApplicationDto(application.Id, application.OpportunityId, application.Opportunity.Title,
            application.ApplicantUserId, user.DisplayName, user.Email ?? string.Empty, application.Message,
            application.PortfolioUrl == string.Empty ? null : application.PortfolioUrl, application.Status, application.CreatedAtUtc);

    private static readonly Expression<Func<WorkspaceOpportunity, OpportunityDto>> OpportunityProjection = x => new(x.Id, x.BusinessId, x.Business.TradingName,
        x.Business.Locations.Where(l => l.IsPrimary).Select(l => l.City).FirstOrDefault() ?? string.Empty,
        x.Business.Locations.Where(l => l.IsPrimary).Select(l => l.Province).FirstOrDefault() ?? string.Empty,
        x.WorkspaceType, x.OpportunityType, x.Status, x.Title, x.Description, x.RequiredSkills, x.IncludedAmenities,
        x.HouseRules, x.SpacesAvailable, x.RateAmount, x.RatePeriod, x.StartDate, x.EndDate, x.Recurring,
        x.BringOwnClients, x.ApprovalRequired, x.ImageUrl, x.ImageUrls, x.Applications.Count, x.CreatedAtUtc,
        !x.Business.IsDemo, x.Business.IsDemo, false, false, null, false);

    private async Task<IReadOnlyCollection<OpportunityDto>> EnrichAsync(OpportunityDto[] items, double? latitude, double? longitude, Guid? userId, CancellationToken ct)
    {
        var ids = items.Select(x => x.Id).ToArray();
        var saved = userId is null ? [] : await db.SavedWorkspaceOpportunities.AsNoTracking()
            .Where(x => x.UserId == userId && ids.Contains(x.OpportunityId)).Select(x => x.OpportunityId).ToArrayAsync(ct);
        var locations = latitude is null || longitude is null ? [] : await db.WorkspaceOpportunities.AsNoTracking()
            .Where(x => ids.Contains(x.Id)).Select(x => new { x.Id, Location = x.Business.Locations.FirstOrDefault(l => l.IsPrimary) })
            .Where(x => x.Location != null && x.Location.Latitude != null && x.Location.Longitude != null)
            .Select(x => new LocationRow(x.Id, x.Location!.Latitude!.Value, x.Location.Longitude!.Value)).ToArrayAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return items.Select(x => x with {
            IsSaved = saved.Contains(x.Id), IsExpired = SpaceEnrichmentPolicy.IsExpired(x.EndDate, today),
            IsUrgent = SpaceEnrichmentPolicy.IsUrgent(x.StartDate, x.EndDate, today),
            DistanceKm = locations.FirstOrDefault(l => l.Id == x.Id) is { } l && latitude is not null && longitude is not null
                ? SpaceEnrichmentPolicy.DistanceKm(latitude.Value, longitude.Value, l.Latitude, l.Longitude) : null
        }).ToArray();
    }

    private static bool IsHttpsUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && value.Length <= 2048;
    private sealed record LocationRow(Guid Id, double Latitude, double Longitude);
    private static GlowBook.Domain.Operations.Notification Notification(Guid userId, string type, string title, string message, string url, string subjectType, Guid subjectId) =>
        new() { Id = Guid.NewGuid(), RecipientUserId = userId, EventType = type, Title = title, Message = message, ActionUrl = url,
            SubjectType = subjectType, SubjectId = subjectId, CreatedAtUtc = DateTime.UtcNow };
}
