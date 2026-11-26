using GlowBook.Application.Professionals;
using GlowBook.Domain.Professionals;
using GlowBook.Domain.Operations;
using GlowBook.Domain.Availability;
using GlowBook.Domain.Bookings;
using GlowBook.Infrastructure.Availability;
using GlowBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GlowBook.Infrastructure.Professionals;

internal sealed class ProfessionalService(GlowBookDbContext db) : IProfessionalService
{
    public async Task<BusinessDto?> GetAsync(Guid ownerId, CancellationToken ct) => Map(await Query().SingleOrDefaultAsync(x => x.OwnerUserId == ownerId, ct));

    public async Task<ProfessionalOnboardingDto> OnboardingAsync(Guid ownerId, CancellationToken ct)
    {
        var accountVerified = await db.Users.AnyAsync(x => x.Id == ownerId && x.EmailConfirmed && x.AccountStatus == "Active", ct);
        var business = await db.Businesses.AsNoTracking()
            .Include(x => x.Locations)
            .Include(x => x.Staff)
            .SingleOrDefaultAsync(x => x.OwnerUserId == ownerId, ct);
        if (business is null)
            return new(accountVerified, false, false, false, false, false, false, false, false, "NotStarted", null);

        var location = business.Locations.SingleOrDefault(x => x.IsPrimary && x.IsActive);
        var businessComplete = !string.IsNullOrWhiteSpace(business.TradingName)
            && !string.IsNullOrWhiteSpace(business.Description)
            && !string.IsNullOrWhiteSpace(business.CancellationPolicy)
            && (!string.IsNullOrWhiteSpace(business.PublicEmail) || !string.IsNullOrWhiteSpace(business.PublicPhone));
        var locationComplete = location is not null
            && !string.IsNullOrWhiteSpace(location.AddressLine1)
            && !string.IsNullOrWhiteSpace(location.Suburb)
            && !string.IsNullOrWhiteSpace(location.City)
            && !string.IsNullOrWhiteSpace(location.Province)
            && !string.IsNullOrWhiteSpace(location.PostalCode);
        var portfolioComplete = await db.PortfolioImages.AnyAsync(x => x.BusinessId == business.Id && x.Status == PortfolioImageStatus.Active, ct);
        var activeStaffIds = business.Staff.Where(x => x.IsActive && x.AcceptingBookings).Select(x => x.Id).ToArray();
        var staffComplete = activeStaffIds.Length > 0;
        var servicesComplete = await db.Services.AnyAsync(x => x.BusinessId == business.Id && x.IsActive, ct);
        var pricingComplete = await db.ServiceOfferings.AnyAsync(x => x.Service.BusinessId == business.Id && x.Service.IsActive && x.IsActive, ct);
        var scheduledStaffIds = await db.WeeklyAvailabilities.AsNoTracking()
            .Where(x => activeStaffIds.Contains(x.StaffMemberId) && x.IsActive)
            .Select(x => x.StaffMemberId)
            .Distinct()
            .ToArrayAsync(ct);
        var availabilityComplete = staffComplete && activeStaffIds.All(scheduledStaffIds.Contains);
        var approvalSubmitted = business.ApprovalStatus is BusinessApprovalStatus.PendingReview or BusinessApprovalStatus.Approved;
        return new(accountVerified, businessComplete, locationComplete, portfolioComplete, staffComplete, servicesComplete,
            pricingComplete, availabilityComplete, approvalSubmitted, business.ApprovalStatus.ToString(), business.ReviewReason);
    }

    public async Task<ProfessionalDashboardDto> DashboardAsync(Guid ownerId, CancellationToken ct)
    {
        var onboarding = await OnboardingAsync(ownerId, ct);
        var completed = new[] { onboarding.AccountVerified, onboarding.BusinessComplete, onboarding.LocationComplete,
            onboarding.PortfolioComplete, onboarding.StaffComplete, onboarding.ServicesComplete,
            onboarding.PricingComplete, onboarding.AvailabilityComplete, onboarding.ApprovalSubmitted }.Count(x => x);
        var business = await db.Businesses.AsNoTracking().SingleOrDefaultAsync(x => x.OwnerUserId == ownerId, ct);
        if (business is null)
            return new(onboarding.ApprovalStatus, onboarding.ReviewReason, completed, 9, 0, 0, 0, 0, 0, 0, []);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg");
        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(localToday.ToDateTime(TimeOnly.MinValue), timeZone);
        var dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(localToday.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone);
        var todayBookings = await db.Bookings.AsNoTracking()
            .Where(x => x.BusinessId == business.Id && x.StartUtc >= dayStartUtc && x.StartUtc < dayEndUtc
                && x.Status != GlowBook.Domain.Bookings.BookingStatus.Cancelled
                && x.Status != GlowBook.Domain.Bookings.BookingStatus.Rejected)
            .OrderBy(x => x.StartUtc)
            .Select(x => new ProfessionalDashboardBookingDto(x.Id,
                db.Users.Where(user => user.Id == x.CustomerUserId).Select(user => user.DisplayName).FirstOrDefault() ?? "Customer",
                x.StaffMember.DisplayName,
                x.ServiceNameSnapshot, x.StartUtc, x.EndUtc, x.Status.ToString()))
            .ToArrayAsync(ct);
        var pendingBookings = await db.Bookings.CountAsync(x => x.BusinessId == business.Id
            && x.Status == GlowBook.Domain.Bookings.BookingStatus.Pending, ct);
        var activeStaffIds = await db.StaffMembers.AsNoTracking()
            .Where(x => x.BusinessId == business.Id && x.IsActive && x.AcceptingBookings)
            .Select(x => x.Id).ToArrayAsync(ct);
        var weeklyStaffIds = await db.WeeklyAvailabilities.AsNoTracking()
            .Where(x => activeStaffIds.Contains(x.StaffMemberId) && x.IsActive && x.DayOfWeek == localToday.DayOfWeek)
            .Select(x => x.StaffMemberId).Distinct().ToArrayAsync(ct);
        var addedStaffIds = await db.AvailabilityExceptions.AsNoTracking()
            .Where(x => activeStaffIds.Contains(x.StaffMemberId) && x.Date == localToday
                && x.Type == GlowBook.Domain.Availability.AvailabilityExceptionType.AddedAvailability)
            .Select(x => x.StaffMemberId).Distinct().ToArrayAsync(ct);
        var unavailableStaffIds = await db.AvailabilityExceptions.AsNoTracking()
            .Where(x => activeStaffIds.Contains(x.StaffMemberId) && x.Date == localToday
                && x.Type == GlowBook.Domain.Availability.AvailabilityExceptionType.Unavailable
                && x.StartTime == null && x.EndTime == null)
            .Select(x => x.StaffMemberId).Distinct().ToArrayAsync(ct);
        var scheduledToday = weeklyStaffIds.Concat(addedStaffIds).Distinct().Except(unavailableStaffIds).Count();
        var publishedSpaces = await db.WorkspaceOpportunities.CountAsync(x => x.BusinessId == business.Id
            && x.Status == GlowBook.Domain.Spaces.OpportunityStatus.Published, ct);
        var pendingApplications = await db.WorkspaceApplications.CountAsync(x => x.Opportunity.BusinessId == business.Id
            && x.Status == GlowBook.Domain.Spaces.ApplicationStatus.Pending, ct);
        return new(onboarding.ApprovalStatus, onboarding.ReviewReason, completed, 9, todayBookings.Length,
            pendingBookings, activeStaffIds.Length, scheduledToday, publishedSpaces, pendingApplications, todayBookings);
    }

    public async Task<ProfessionalInsightsDto> InsightsAsync(Guid ownerId, int periodDays, CancellationToken ct)
    {
        periodDays = periodDays is 30 or 90 ? periodDays : 30;
        var business = await db.Businesses.AsNoTracking().Include(x => x.Locations)
            .SingleOrDefaultAsync(x => x.OwnerUserId == ownerId, ct);
        var zoneId = business?.Locations.Single(x => x.IsPrimary).TimeZone ?? "Africa/Johannesburg";
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone));
        var from = today.AddDays(-(periodDays - 1));
        if (business is null) return new(periodDays, from, today, 0, 0, 0, 0, null, false, 0, 0, 0, []);
        var rangeStart = TimeZoneInfo.ConvertTimeToUtc(from.ToDateTime(TimeOnly.MinValue), zone);
        var rangeEnd = TimeZoneInfo.ConvertTimeToUtc(today.AddDays(1).ToDateTime(TimeOnly.MinValue), zone);
        var bookings = await db.Bookings.AsNoTracking()
            .Where(x => x.BusinessId == business.Id && x.StartUtc >= rangeStart && x.StartUtc < rangeEnd)
            .Select(x => new { x.CustomerUserId, x.StartUtc, x.EndUtc, x.Status }).ToArrayAsync(ct);
        var activeStaffIds = await db.StaffMembers.AsNoTracking()
            .Where(x => x.BusinessId == business.Id && x.IsActive && x.AcceptingBookings).Select(x => x.Id).ToArrayAsync(ct);
        var weekly = await db.WeeklyAvailabilities.AsNoTracking()
            .Where(x => activeStaffIds.Contains(x.StaffMemberId) && x.IsActive).ToArrayAsync(ct);
        var exceptions = await db.AvailabilityExceptions.AsNoTracking()
            .Where(x => activeStaffIds.Contains(x.StaffMemberId) && x.Date >= from && x.Date <= today).ToArrayAsync(ct);
        var blocked = await db.BlockedTimes.AsNoTracking()
            .Where(x => activeStaffIds.Contains(x.StaffMemberId) && x.StartUtc < rangeEnd && x.EndUtc > rangeStart)
            .Select(x => new { x.StaffMemberId, x.StartUtc, x.EndUtc }).ToArrayAsync(ct);
        var capacityMinutes = 0;
        for (var date = from; date <= today; date = date.AddDays(1))
            foreach (var id in activeStaffIds)
                foreach (var window in AvailabilityWindowCalculator.ForDate(date,
                    weekly.Where(x => x.StaffMemberId == id), exceptions.Where(x => x.StaffMemberId == id)))
                {
                    var windowStart = TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(window.Start), zone);
                    var windowEnd = TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(window.End), zone);
                    var blockedMinutes = blocked.Where(x => x.StaffMemberId == id)
                        .Sum(x => OverlapMinutes(x.StartUtc, x.EndUtc, windowStart, windowEnd));
                    capacityMinutes += Math.Max(0, (int)(windowEnd - windowStart).TotalMinutes - blockedMinutes);
                }
        var capacityStatuses = new[] { BookingStatus.Pending, BookingStatus.Confirmed, BookingStatus.Completed, BookingStatus.NoShow };
        var bookedMinutes = bookings.Where(x => capacityStatuses.Contains(x.Status)).Sum(x => (int)(x.EndUtc - x.StartUtc).TotalMinutes);
        var distinctCustomers = bookings.Select(x => x.CustomerUserId).Distinct().Count();
        var thresholdMet = distinctCustomers >= ProfessionalInsightPolicy.MinimumCustomerCohort;
        var repeatCustomers = ProfessionalInsightPolicy.RepeatCustomerCount(
            bookings.Where(x => x.Status == BookingStatus.Completed).Select(x => x.CustomerUserId), distinctCustomers);
        var daily = Enumerable.Range(0, periodDays).Select(offset => from.AddDays(offset)).Select(date =>
        {
            var items = bookings.Where(x => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(x.StartUtc, zone)) == date).ToArray();
            return new ProfessionalInsightPointDto(date, items.Length, items.Count(x => x.Status == BookingStatus.Completed),
                items.Count(x => x.Status == BookingStatus.Cancelled), items.Count(x => x.Status == BookingStatus.NoShow));
        }).ToArray();
        return new(periodDays, from, today, bookings.Length, bookings.Count(x => x.Status == BookingStatus.Completed),
            bookings.Count(x => x.Status == BookingStatus.Cancelled), bookings.Count(x => x.Status == BookingStatus.NoShow),
            repeatCustomers, thresholdMet, capacityMinutes, bookedMinutes,
            ProfessionalInsightPolicy.UtilisationPercent(bookedMinutes, capacityMinutes), daily);
    }

    public async Task<BusinessDto> SaveAsync(Guid ownerId, string ownerName, BusinessRequest request, CancellationToken ct)
    {
        var business = await Query().SingleOrDefaultAsync(x => x.OwnerUserId == ownerId, ct);
        if (business is null)
        {
            business = new Business { Id = Guid.NewGuid(), OwnerUserId = ownerId, CreatedAtUtc = DateTime.UtcNow };
            business.Locations.Add(new BusinessLocation { Id = Guid.NewGuid() });
            business.Staff.Add(new StaffMember { Id = Guid.NewGuid(), LinkedUserId = ownerId, DisplayName = ownerName, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
            db.Businesses.Add(business);
        }
        else if (request.ConcurrencyToken != business.ConcurrencyToken) throw new DbUpdateConcurrencyException("The profile was changed by another request.");
        business.TradingName = request.TradingName.Trim(); business.Description = request.Description?.Trim() ?? string.Empty; business.CancellationPolicy = request.CancellationPolicy?.Trim() ?? string.Empty; business.PublicEmail = request.PublicEmail?.Trim(); business.PublicPhone = request.PublicPhone?.Trim(); business.UpdatedAtUtc = DateTime.UtcNow; business.ConcurrencyToken = Guid.NewGuid();
        var location = business.Locations.Single(x => x.IsPrimary); location.AddressLine1 = request.Location.AddressLine1.Trim(); location.AddressLine2 = request.Location.AddressLine2?.Trim(); location.Suburb = request.Location.Suburb.Trim(); location.City = request.Location.City.Trim(); location.Province = request.Location.Province.Trim(); location.PostalCode = request.Location.PostalCode.Trim(); location.Latitude = request.Location.Latitude; location.Longitude = request.Location.Longitude;
        if (business.ApprovalStatus == BusinessApprovalStatus.Approved) business.ApprovalStatus = BusinessApprovalStatus.PendingReview;
        await db.SaveChangesAsync(ct); return Map(business)!;
    }

    public async Task<bool> SubmitAsync(Guid ownerId, CancellationToken ct)
    {
        var onboarding = await OnboardingAsync(ownerId, ct);
        if (!onboarding.AccountVerified || !onboarding.BusinessComplete || !onboarding.LocationComplete
            || !onboarding.PortfolioComplete || !onboarding.StaffComplete || !onboarding.ServicesComplete
            || !onboarding.PricingComplete || !onboarding.AvailabilityComplete) return false;
        var business = await Query().SingleOrDefaultAsync(x => x.OwnerUserId == ownerId, ct);
        if (business is null || string.IsNullOrWhiteSpace(business.TradingName) || business.Locations.Count == 0) return false;
        business.ApprovalStatus = BusinessApprovalStatus.PendingReview; business.SubmittedAtUtc = DateTime.UtcNow; business.ReviewReason = null;
        db.AuditLogs.Add(Audit(ownerId, "BusinessSubmitted", business.Id, null)); await db.SaveChangesAsync(ct); return true;
    }

    public async Task<StaffDto?> AddStaffAsync(Guid ownerId, StaffRequest request, CancellationToken ct)
    {
        var businessId = await db.Businesses.Where(x => x.OwnerUserId == ownerId).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (businessId is null) return null;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({businessId.Value.ToString()}))", ct);
        var business = await db.Businesses.Include(x => x.Staff).SingleAsync(x => x.Id == businessId.Value, ct);
        if (business is null || business.Staff.Count >= 25) return null;
        var staff = new StaffMember { Id = Guid.NewGuid(), BusinessId = business.Id, DisplayName = request.DisplayName.Trim(), Biography = request.Biography?.Trim() ?? string.Empty, WorkspaceName = NormaliseWorkspace(request.WorkspaceName), AcceptingBookings = request.AcceptingBookings, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        db.StaffMembers.Add(staff); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return MapStaff(staff);
    }

    public async Task<StaffDto?> UpdateStaffAsync(Guid ownerId, Guid staffId, StaffRequest request, CancellationToken ct)
    {
        var staff = await db.StaffMembers.SingleOrDefaultAsync(x => x.Id == staffId && x.Business.OwnerUserId == ownerId, ct);
        if (staff is null) return null;
        staff.DisplayName = request.DisplayName.Trim();
        staff.Biography = request.Biography?.Trim() ?? string.Empty;
        staff.WorkspaceName = NormaliseWorkspace(request.WorkspaceName);
        staff.AcceptingBookings = request.AcceptingBookings;
        staff.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapStaff(staff);
    }

    public async Task<IReadOnlyCollection<BusinessDto>> PendingAsync(CancellationToken ct) => (await Query().Where(x => x.ApprovalStatus == BusinessApprovalStatus.PendingReview).OrderBy(x => x.SubmittedAtUtc).Take(100).ToListAsync(ct)).Select(x => Map(x)!).ToArray();
    public async Task<IReadOnlyCollection<BusinessDto>> ManagedAsync(CancellationToken ct) => (await Query().OrderBy(x => x.TradingName).Take(200).ToListAsync(ct)).Select(x => Map(x)!).ToArray();
    public async Task<bool> ReviewAsync(Guid actorId, Guid id, BusinessApprovalStatus status, string? reason, CancellationToken ct) { var b = await db.Businesses.FindAsync([id], ct); if (b is null) return false; b.ApprovalStatus = status; b.ReviewReason = reason; b.ApprovedAtUtc = status == BusinessApprovalStatus.Approved ? DateTime.UtcNow : null; b.UpdatedAtUtc = DateTime.UtcNow; db.AuditLogs.Add(Audit(actorId, $"Business{status}", id, reason)); await db.SaveChangesAsync(ct); return true; }
    public async Task<bool> SetDemoAsync(Guid actorId, Guid id, bool isDemo, CancellationToken ct) { var b = await db.Businesses.FindAsync([id], ct); if (b is null) return false; b.IsDemo = isDemo; b.UpdatedAtUtc = DateTime.UtcNow; db.AuditLogs.Add(Audit(actorId, isDemo ? "BusinessMarkedDemo" : "BusinessMarkedReal", id, null)); await db.SaveChangesAsync(ct); return true; }
    public async Task<PortfolioImageDto?> AddPortfolioImageAsync(Guid ownerId, PortfolioImageRequest request, CancellationToken ct) { var businessId = await db.Businesses.Where(x => x.OwnerUserId == ownerId).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct); if (businessId is null) return null; await using var transaction = await db.Database.BeginTransactionAsync(ct); await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({businessId.Value.ToString()}))", ct); if (await db.PortfolioImages.CountAsync(x => x.BusinessId == businessId, ct) >= 10 || await db.PortfolioImages.AnyAsync(x => x.BusinessId == businessId && x.DisplayOrder == request.DisplayOrder, ct)) return null; var image = new PortfolioImage { Id = Guid.NewGuid(), BusinessId = businessId.Value, ImageUrl = request.ImageUrl.Trim(), AltText = request.AltText.Trim(), DisplayOrder = request.DisplayOrder, CreatedAtUtc = DateTime.UtcNow }; db.PortfolioImages.Add(image); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return new(image.Id, image.ImageUrl, image.AltText, image.DisplayOrder, image.Status); }
    public async Task<IReadOnlyCollection<PortfolioImageDto>> PortfolioImagesAsync(Guid ownerId, CancellationToken ct) => await db.PortfolioImages.AsNoTracking().Where(x => x.Business.OwnerUserId == ownerId).OrderBy(x => x.DisplayOrder).Select(x => new PortfolioImageDto(x.Id, x.ImageUrl, x.AltText, x.DisplayOrder, x.Status)).ToListAsync(ct);
    public async Task<bool> DeletePortfolioImageAsync(Guid ownerId, Guid imageId, CancellationToken ct) { var image = await db.PortfolioImages.SingleOrDefaultAsync(x => x.Id == imageId && x.Business.OwnerUserId == ownerId, ct); if (image is null) return false; db.PortfolioImages.Remove(image); await db.SaveChangesAsync(ct); return true; }
    public async Task<bool> ModeratePortfolioImageAsync(Guid actorId, Guid imageId, bool hidden, CancellationToken ct) { var image = await db.PortfolioImages.SingleOrDefaultAsync(x => x.Id == imageId, ct); if (image is null) return false; image.Status = hidden ? PortfolioImageStatus.Hidden : PortfolioImageStatus.Active; db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = actorId, Action = hidden ? "PortfolioImageHidden" : "PortfolioImageRestored", SubjectType = "PortfolioImage", SubjectId = imageId, OccurredAtUtc = DateTime.UtcNow }); await db.SaveChangesAsync(ct); return true; }
    public async Task<IReadOnlyCollection<AdminPortfolioImageDto>> PortfolioModerationAsync(CancellationToken ct) => await db.PortfolioImages.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(100).Select(x => new AdminPortfolioImageDto(x.Id, x.Business.TradingName, x.ImageUrl, x.AltText, x.Status, x.CreatedAtUtc)).ToListAsync(ct);
    private IQueryable<Business> Query() => db.Businesses.Include(x => x.Locations).Include(x => x.Staff);
    private static BusinessDto? Map(Business? b) { if (b is null) return null; var l = b.Locations.Single(x => x.IsPrimary); return new(b.Id, b.TradingName, b.Description, b.CancellationPolicy, b.PublicEmail, b.PublicPhone, b.IsDemo, b.ApprovalStatus, b.ReviewReason, b.ConcurrencyToken, new(l.AddressLine1, l.AddressLine2, l.Suburb, l.City, l.Province, l.PostalCode, l.Latitude, l.Longitude), b.Staff.Select(MapStaff).ToArray()); }
    private static StaffDto MapStaff(StaffMember staff) => new(staff.Id, staff.DisplayName, staff.Biography, staff.WorkspaceName, staff.IsActive, staff.AcceptingBookings);
    private static string? NormaliseWorkspace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static int OverlapMinutes(DateTime start, DateTime end, DateTime rangeStart, DateTime rangeEnd)
        => Math.Max(0, (int)(new[] { end, rangeEnd }.Min() - new[] { start, rangeStart }.Max()).TotalMinutes);
    private static AuditLog Audit(Guid actorId, string action, Guid subjectId, string? details) => new() { Id = Guid.NewGuid(), ActorUserId = actorId, Action = action, SubjectType = "Business", SubjectId = subjectId, Details = details, OccurredAtUtc = DateTime.UtcNow };
}
