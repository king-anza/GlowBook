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

    public async Task<WorkspaceApplicantProfileDto?> ApplicantProfileAsync(Guid userId, CancellationToken ct) =>
        await db.WorkspaceApplicantProfiles.AsNoTracking().Where(x => x.UserId == userId).Select(ProfileProjection).SingleOrDefaultAsync(ct);

    public async Task<WorkspaceApplicantProfileDto?> SaveApplicantProfileAsync(Guid userId, WorkspaceApplicantProfileRequest r, CancellationToken ct)
    {
        var cities = r.PreferredCities.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToArray();
        if (r.PortfolioUrl is not null && !IsHttpsUrl(r.PortfolioUrl) || r.PreferredOpportunityTypes.Distinct().Count() != r.PreferredOpportunityTypes.Length) return null;
        var item = await db.WorkspaceApplicantProfiles.SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (item is null) { item = new WorkspaceApplicantProfile { UserId = userId }; db.WorkspaceApplicantProfiles.Add(item); }
        item.Summary = r.Summary.Trim(); item.Skills = r.Skills.Trim(); item.Availability = r.Availability.Trim();
        item.AvailableFrom = r.AvailableFrom; item.PortfolioUrl = r.PortfolioUrl?.Trim() ?? string.Empty;
        item.PreferredCities = cities; item.PreferredOpportunityTypes = r.PreferredOpportunityTypes.Distinct().ToArray();
        item.MatchingAlertsEnabled = r.MatchingAlertsEnabled; item.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await ApplicantProfileAsync(userId, ct);
    }

    public async Task<IReadOnlyCollection<MatchedOpportunityDto>> MatchesAsync(Guid userId, CancellationToken ct)
    {
        var profile = await db.WorkspaceApplicantProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (profile is null) return [];
        var opportunities = await db.WorkspaceOpportunities.AsNoTracking()
            .Where(x => x.Status == OpportunityStatus.Published && x.Business.ApprovalStatus == GlowBook.Domain.Professionals.BusinessApprovalStatus.Approved)
            .OrderByDescending(x => x.CreatedAtUtc).Select(OpportunityProjection).ToArrayAsync(ct);
        var enriched = await EnrichAsync(opportunities, null, null, userId, ct);
        return enriched.Select(x =>
        {
            var match = SpaceMatchingPolicy.Evaluate(x.RequiredSkills, x.City, x.StartDate, x.EndDate, x.OpportunityType,
                profile.Skills, profile.PreferredCities, profile.AvailableFrom, profile.PreferredOpportunityTypes);
            return new MatchedOpportunityDto(x, match.Score, match.Reasons);
        }).Where(x => x.MatchScore >= 30).OrderByDescending(x => x.MatchScore).ThenByDescending(x => x.Opportunity.CreatedAtUtc).ToArray();
    }

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
        var previous = item.Status; item.Status = status; item.UpdatedAtUtc = DateTime.UtcNow;
        if (status == OpportunityStatus.Filled)
        {
            var accepted = await db.WorkspaceApplications.Where(x => x.OpportunityId == id && x.Status == ApplicationStatus.Accepted).ToArrayAsync(ct);
            foreach (var application in accepted)
            {
                application.Status = ApplicationStatus.Filled; application.UpdatedAtUtc = DateTime.UtcNow;
                AddHistory(application, ApplicationStatus.Filled, ownerId);
                db.Notifications.Add(Notification(application.ApplicantUserId, "ApplicationFilled", "Opportunity filled",
                    $"The opportunity {item.Title} has been filled.", $"/spaces?application={application.Id}", "WorkspaceApplication", application.Id));
            }
        }
        if (status == OpportunityStatus.Published && previous != OpportunityStatus.Published) await AddMatchAlertsAsync(item, ct);
        await db.SaveChangesAsync(ct); return true;
    }

    public async Task<ApplicationDto?> ApplyAsync(Guid userId, Guid opportunityId, ApplicationRequest r, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var opportunity = await db.WorkspaceOpportunities.Include(x => x.Business).SingleOrDefaultAsync(x => x.Id == opportunityId && x.Status == OpportunityStatus.Published && (x.EndDate == null || x.EndDate >= today), ct);
        if (opportunity?.Business.OwnerUserId == userId) return null;
        if (opportunity is null || await db.WorkspaceApplications.AnyAsync(x => x.OpportunityId == opportunityId && x.ApplicantUserId == userId, ct)) return null;
        var profile = await db.WorkspaceApplicantProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (profile is null) return null;
        var item = new WorkspaceApplication { Id = Guid.NewGuid(), OpportunityId = opportunityId, ApplicantUserId = userId,
            Message = r.Message.Trim(), ProfileSummary = profile.Summary, Skills = profile.Skills, Availability = profile.Availability,
            PortfolioUrl = string.IsNullOrWhiteSpace(profile.PortfolioUrl) ? r.PortfolioUrl?.Trim() ?? string.Empty : profile.PortfolioUrl,
            Status = ApplicationStatus.Submitted, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        db.WorkspaceApplications.Add(item);
        AddHistory(item, ApplicationStatus.Submitted, userId);
        db.Notifications.Add(Notification(opportunity.Business.OwnerUserId, "ApplicationReceived", "New Glow Spaces application",
            $"A professional applied for {opportunity.Title}.", $"/professional/spaces?application={item.Id}", "WorkspaceApplication", item.Id));
        await db.SaveChangesAsync(ct);
        return (await ApplicationsAsync(applicationId: item.Id, ct: ct)).Single();
    }

    public async Task<IReadOnlyCollection<ApplicationDto>> OwnerApplicationsAsync(Guid ownerId, CancellationToken ct) => await ApplicationsAsync(ownerId: ownerId, ct: ct);
    public async Task<IReadOnlyCollection<ApplicationDto>> ApplicantApplicationsAsync(Guid userId, CancellationToken ct) => await ApplicationsAsync(applicantId: userId, ct: ct);
    public async Task<bool> ReviewApplicationAsync(Guid ownerId, Guid applicationId, ApplicationStatus status, CancellationToken ct)
    {
        if (status is not (ApplicationStatus.Accepted or ApplicationStatus.Declined)) return false;
        var item = await db.WorkspaceApplications.Include(x => x.Opportunity).ThenInclude(x => x.Business).SingleOrDefaultAsync(x => x.Id == applicationId && x.Opportunity.Business.OwnerUserId == ownerId, ct);
        if (item is null || item.Status is not (ApplicationStatus.Submitted or ApplicationStatus.Viewed)) return false;
        item.Status = status; item.UpdatedAtUtc = DateTime.UtcNow;
        AddHistory(item, status, ownerId);
        db.Notifications.Add(Notification(item.ApplicantUserId, $"Application{status}", $"Application {status.ToString().ToLowerInvariant()}",
            $"Your application for {item.Opportunity.Title} was {status.ToString().ToLowerInvariant()}.", $"/spaces?opportunity={item.OpportunityId}", "WorkspaceApplication", item.Id));
        await db.SaveChangesAsync(ct); return true;
    }

    public async Task<bool> MarkApplicationViewedAsync(Guid ownerId, Guid applicationId, CancellationToken ct)
    {
        var item = await db.WorkspaceApplications.Include(x => x.Opportunity).ThenInclude(x => x.Business)
            .SingleOrDefaultAsync(x => x.Id == applicationId && x.Opportunity.Business.OwnerUserId == ownerId, ct);
        if (item is null) return false;
        if (item.Status != ApplicationStatus.Submitted) return true;
        item.Status = ApplicationStatus.Viewed; item.UpdatedAtUtc = DateTime.UtcNow; AddHistory(item, ApplicationStatus.Viewed, ownerId);
        db.Notifications.Add(Notification(item.ApplicantUserId, "ApplicationViewed", "Application viewed",
            $"{item.Opportunity.Business.TradingName} viewed your application for {item.Opportunity.Title}.", $"/spaces?application={item.Id}", "WorkspaceApplication", item.Id));
        await db.SaveChangesAsync(ct); return true;
    }

    public async Task<bool> WithdrawApplicationAsync(Guid userId, Guid applicationId, CancellationToken ct)
    {
        var item = await db.WorkspaceApplications.Include(x => x.Opportunity).ThenInclude(x => x.Business)
            .SingleOrDefaultAsync(x => x.Id == applicationId && x.ApplicantUserId == userId, ct);
        if (item is null || item.Status is not (ApplicationStatus.Submitted or ApplicationStatus.Viewed)) return false;
        item.Status = ApplicationStatus.Withdrawn; item.UpdatedAtUtc = DateTime.UtcNow; AddHistory(item, ApplicationStatus.Withdrawn, userId);
        db.Notifications.Add(Notification(item.Opportunity.Business.OwnerUserId, "ApplicationWithdrawn", "Application withdrawn",
            $"An application for {item.Opportunity.Title} was withdrawn.", $"/professional/spaces?application={item.Id}", "WorkspaceApplication", item.Id));
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

    private async Task<IReadOnlyCollection<ApplicationDto>> ApplicationsAsync(Guid? ownerId = null, Guid? applicantId = null,
        Guid? applicationId = null, CancellationToken ct = default)
    {
        var query = db.WorkspaceApplications.AsNoTracking().Include(x => x.Opportunity).ThenInclude(x => x.Business).AsQueryable();
        if (ownerId is not null) query = query.Where(x => x.Opportunity.Business.OwnerUserId == ownerId);
        if (applicantId is not null) query = query.Where(x => x.ApplicantUserId == applicantId);
        if (applicationId is not null) query = query.Where(x => x.Id == applicationId);
        var applications = await query.OrderByDescending(x => x.CreatedAtUtc).ToArrayAsync(ct);
        var ids = applications.Select(x => x.Id).ToArray();
        var userIds = applications.Select(x => x.ApplicantUserId).Distinct().ToArray();
        var users = await db.Users.AsNoTracking().Where(x => userIds.Contains(x.Id))
            .Select(x => new { x.Id, x.DisplayName, x.Email }).ToDictionaryAsync(x => x.Id, ct);
        var events = await db.WorkspaceApplicationStatusHistory.AsNoTracking().Where(x => ids.Contains(x.ApplicationId))
            .OrderBy(x => x.ChangedAtUtc).Select(x => new { x.ApplicationId, Event = new ApplicationStatusEventDto(x.Status, x.ChangedAtUtc) }).ToArrayAsync(ct);
        return applications.Select(x => new ApplicationDto(x.Id, x.OpportunityId, x.Opportunity.Title, x.ApplicantUserId,
            users[x.ApplicantUserId].DisplayName, x.Status is ApplicationStatus.Accepted or ApplicationStatus.Filled ? users[x.ApplicantUserId].Email : null,
            x.Message, x.ProfileSummary, x.Skills, x.Availability, string.IsNullOrWhiteSpace(x.PortfolioUrl) ? null : x.PortfolioUrl,
            x.Status, x.CreatedAtUtc, x.UpdatedAtUtc, events.Where(e => e.ApplicationId == x.Id).Select(e => e.Event).ToArray())).ToArray();
    }

    private static readonly Expression<Func<WorkspaceApplicantProfile, WorkspaceApplicantProfileDto>> ProfileProjection = x =>
        new(x.Summary, x.Skills, x.Availability, x.AvailableFrom, x.PortfolioUrl == string.Empty ? null : x.PortfolioUrl,
            x.PreferredCities, x.PreferredOpportunityTypes, x.MatchingAlertsEnabled, x.UpdatedAtUtc);

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
    private void AddHistory(WorkspaceApplication application, ApplicationStatus status, Guid userId) =>
        db.WorkspaceApplicationStatusHistory.Add(new() { Id = Guid.NewGuid(), Application = application, ApplicationId = application.Id,
            Status = status, ChangedByUserId = userId, ChangedAtUtc = DateTime.UtcNow });

    private async Task AddMatchAlertsAsync(WorkspaceOpportunity item, CancellationToken ct)
    {
        var city = await db.BusinessLocations.AsNoTracking().Where(x => x.BusinessId == item.BusinessId && x.IsPrimary).Select(x => x.City).FirstOrDefaultAsync(ct) ?? string.Empty;
        var profiles = await db.WorkspaceApplicantProfiles.AsNoTracking().Where(x => x.MatchingAlertsEnabled && x.UserId != item.Business.OwnerUserId).ToArrayAsync(ct);
        foreach (var profile in profiles)
        {
            var match = SpaceMatchingPolicy.Evaluate(item.RequiredSkills, city, item.StartDate, item.EndDate, item.OpportunityType,
                profile.Skills, profile.PreferredCities, profile.AvailableFrom, profile.PreferredOpportunityTypes);
            if (match.Score < 60 || await db.Notifications.AnyAsync(x => x.RecipientUserId == profile.UserId && x.EventType == "SpaceMatch" && x.SubjectId == item.Id, ct)) continue;
            db.Notifications.Add(Notification(profile.UserId, "SpaceMatch", "A Glow Space matches your profile",
                $"{item.Title}: {string.Join(" · ", match.Reasons.Take(2))}.", $"/spaces?opportunity={item.Id}", "WorkspaceOpportunity", item.Id));
        }
    }
    private sealed record LocationRow(Guid Id, double Latitude, double Longitude);
    private static GlowBook.Domain.Operations.Notification Notification(Guid userId, string type, string title, string message, string url, string subjectType, Guid subjectId) =>
        new() { Id = Guid.NewGuid(), RecipientUserId = userId, EventType = type, Title = title, Message = message, ActionUrl = url,
            SubjectType = subjectType, SubjectId = subjectId, CreatedAtUtc = DateTime.UtcNow };
}
