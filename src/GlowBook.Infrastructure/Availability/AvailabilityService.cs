using GlowBook.Application.Availability;
using GlowBook.Domain.Availability;
using GlowBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using GlowBook.Domain.Bookings;
using GlowBook.Domain.Professionals;

namespace GlowBook.Infrastructure.Availability;

internal sealed class AvailabilityService(GlowBookDbContext db) : IAvailabilityService
{
    public async Task<IReadOnlyCollection<AvailabilityDto>> GetWeeklyAsync(Guid ownerId, Guid staffId, CancellationToken ct) => await db.WeeklyAvailabilities.Where(x => x.StaffMemberId == staffId && x.StaffMember.Business.OwnerUserId == ownerId && x.IsActive).OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime).Select(x => new AvailabilityDto(x.Id, x.DayOfWeek, x.StartTime, x.EndTime)).ToArrayAsync(ct);
    public async Task<bool> ReplaceWeeklyAsync(Guid ownerId, Guid staffId, IReadOnlyCollection<WeeklyAvailabilityRequest> requests, CancellationToken ct)
    {
        if (requests.Count > 14 || requests.Any(x => !Enum.IsDefined(x.DayOfWeek) || x.StartTime >= x.EndTime) || HasOverlap(requests)) return false;
        if (!await db.StaffMembers.AnyAsync(x => x.Id == staffId && x.Business.OwnerUserId == ownerId, ct)) return false;
        db.WeeklyAvailabilities.RemoveRange(db.WeeklyAvailabilities.Where(x => x.StaffMemberId == staffId));
        db.WeeklyAvailabilities.AddRange(requests.Select(x => new WeeklyAvailability { Id = Guid.NewGuid(), StaffMemberId = staffId, DayOfWeek = x.DayOfWeek, StartTime = x.StartTime, EndTime = x.EndTime }));
        await db.SaveChangesAsync(ct); return true;
    }
    public async Task<bool> AddExceptionAsync(Guid ownerId, Guid staffId, ExceptionRequest request, CancellationToken ct) { var hasBothTimes = request.StartTime.HasValue && request.EndTime.HasValue; var fullDayUnavailable = request.Type == AvailabilityExceptionType.Unavailable && !request.StartTime.HasValue && !request.EndTime.HasValue; if (!Enum.IsDefined(request.Type) || !await OwnsStaff(ownerId, staffId, ct) || !fullDayUnavailable && (!hasBothTimes || request.StartTime >= request.EndTime)) return false; db.AvailabilityExceptions.Add(new AvailabilityException { Id = Guid.NewGuid(), StaffMemberId = staffId, Date = request.Date, StartTime = request.StartTime, EndTime = request.EndTime, Type = request.Type, Reason = request.Reason?.Trim() }); await db.SaveChangesAsync(ct); return true; }
    public async Task<bool> AddBlockedTimeAsync(Guid ownerId, Guid staffId, BlockedTimeRequest request, CancellationToken ct)
        => await AddBlockedTimeWithConflictCheckAsync(ownerId, staffId, request, ct) == BlockedTimeResult.Added;

    public async Task<BlockedTimeResult> AddBlockedTimeWithConflictCheckAsync(Guid ownerId, Guid staffId, BlockedTimeRequest request, CancellationToken ct)
    {
        var start = request.StartUtc.UtcDateTime;
        var end = request.EndUtc.UtcDateTime;
        if (!await OwnsStaff(ownerId, staffId, ct)) return BlockedTimeResult.NotOwned;
        if (start >= end || end - start > TimeSpan.FromDays(31)) return BlockedTimeResult.InvalidRange;
        var conflicts = await db.Bookings.AnyAsync(x => x.StaffMemberId == staffId
            && (x.Status == BookingStatus.Pending || x.Status == BookingStatus.Confirmed)
            && x.StartUtc < end && x.EndUtc > start, ct);
        if (conflicts) return BlockedTimeResult.BookingConflict;
        db.BlockedTimes.Add(new BlockedTime { Id = Guid.NewGuid(), StaffMemberId = staffId, StartUtc = start, EndUtc = end, Reason = request.Reason?.Trim(), CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        return BlockedTimeResult.Added;
    }

    public async Task<bool> DeleteBlockedTimeAsync(Guid ownerId, Guid blockedTimeId, CancellationToken ct)
    {
        var item = await db.BlockedTimes.SingleOrDefaultAsync(x => x.Id == blockedTimeId && x.StaffMember.Business.OwnerUserId == ownerId, ct);
        if (item is null) return false;
        db.BlockedTimes.Remove(item);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ProfessionalCalendarDto?> CalendarAsync(Guid ownerId, DateOnly from, DateOnly to, Guid? staffId, CancellationToken ct)
    {
        if (to < from || to.DayNumber - from.DayNumber > 6) return null;
        var business = await db.Businesses.AsNoTracking().Include(x => x.Locations)
            .SingleOrDefaultAsync(x => x.OwnerUserId == ownerId, ct);
        if (business is null) return null;
        var zoneId = business.Locations.Single(x => x.IsPrimary).TimeZone;
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        var rangeStart = TimeZoneInfo.ConvertTimeToUtc(from.ToDateTime(TimeOnly.MinValue), zone);
        var rangeEnd = TimeZoneInfo.ConvertTimeToUtc(to.AddDays(1).ToDateTime(TimeOnly.MinValue), zone);
        var staff = await db.StaffMembers.AsNoTracking()
            .Where(x => x.BusinessId == business.Id && x.IsActive && (!staffId.HasValue || x.Id == staffId))
            .OrderBy(x => x.DisplayName)
            .Select(x => new ProfessionalCalendarStaffDto(x.Id, x.DisplayName, x.WorkspaceName))
            .ToArrayAsync(ct);
        if (staffId.HasValue && staff.All(x => x.Id != staffId.Value)) return null;
        var staffIds = staff.Select(x => x.Id).ToArray();
        var weekly = await db.WeeklyAvailabilities.AsNoTracking().Where(x => staffIds.Contains(x.StaffMemberId) && x.IsActive).ToArrayAsync(ct);
        var exceptions = await db.AvailabilityExceptions.AsNoTracking()
            .Where(x => staffIds.Contains(x.StaffMemberId) && x.Date >= from && x.Date <= to).ToArrayAsync(ct);
        var bookings = await db.Bookings.AsNoTracking()
            .Where(x => x.BusinessId == business.Id && staffIds.Contains(x.StaffMemberId)
                && x.StartUtc < rangeEnd && x.EndUtc > rangeStart
                && x.Status != BookingStatus.Cancelled && x.Status != BookingStatus.Rejected)
            .OrderBy(x => x.StartUtc)
            .Select(x => new { x.Id, x.StaffMemberId, StaffName = x.StaffMember.DisplayName, x.StaffMember.WorkspaceName,
                x.ServiceNameSnapshot, x.StartUtc, x.EndUtc, Status = x.Status.ToString() })
            .ToArrayAsync(ct);
        var blocked = await db.BlockedTimes.AsNoTracking()
            .Where(x => staffIds.Contains(x.StaffMemberId) && x.StartUtc < rangeEnd && x.EndUtc > rangeStart)
            .OrderBy(x => x.StartUtc)
            .Select(x => new { x.Id, x.StaffMemberId, StaffName = x.StaffMember.DisplayName, x.StaffMember.WorkspaceName,
                x.Reason, x.StartUtc, x.EndUtc })
            .ToArrayAsync(ct);

        var bookingEvents = bookings.Select(booking =>
        {
            var blockConflict = blocked.Any(block => block.StaffMemberId == booking.StaffMemberId && Overlaps(block.StartUtc, block.EndUtc, booking.StartUtc, booking.EndUtc));
            var workspaceConflict = !string.IsNullOrWhiteSpace(booking.WorkspaceName) && bookings.Any(other => other.Id != booking.Id
                && other.WorkspaceName == booking.WorkspaceName && other.StaffMemberId != booking.StaffMemberId
                && Overlaps(other.StartUtc, other.EndUtc, booking.StartUtc, booking.EndUtc));
            var conflict = blockConflict ? "This booking overlaps blocked time."
                : workspaceConflict ? $"{booking.WorkspaceName} is allocated to overlapping bookings." : null;
            return new ProfessionalCalendarEventDto(booking.Id, "Booking", booking.ServiceNameSnapshot, booking.StaffMemberId,
                booking.StaffName, booking.WorkspaceName, booking.StartUtc, booking.EndUtc, booking.Status, false, conflict);
        });
        var blockedEvents = blocked.Select(block => new ProfessionalCalendarEventDto(block.Id, "BlockedTime",
            string.IsNullOrWhiteSpace(block.Reason) ? "Blocked time" : block.Reason, block.StaffMemberId, block.StaffName,
            block.WorkspaceName, block.StartUtc, block.EndUtc, "Blocked", true,
            bookings.Any(booking => booking.StaffMemberId == block.StaffMemberId && Overlaps(block.StartUtc, block.EndUtc, booking.StartUtc, booking.EndUtc))
                ? "This block overlaps an active booking." : null));
        var events = bookingEvents.Concat(blockedEvents).OrderBy(x => x.StartUtc).ToArray();

        var days = new List<ProfessionalCalendarDayDto>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var dayStart = TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), zone);
            var dayEnd = TimeZoneInfo.ConvertTimeToUtc(date.AddDays(1).ToDateTime(TimeOnly.MinValue), zone);
            var scheduledMinutes = staff.Sum(member => AvailabilityWindowCalculator.ForDate(date,
                weekly.Where(x => x.StaffMemberId == member.Id), exceptions.Where(x => x.StaffMemberId == member.Id))
                .Sum(window => (int)(window.End - window.Start).TotalMinutes));
            var dayBookings = bookings.Where(x => Overlaps(x.StartUtc, x.EndUtc, dayStart, dayEnd)).ToArray();
            var bookedMinutes = dayBookings.Sum(x => OverlapMinutes(x.StartUtc, x.EndUtc, dayStart, dayEnd));
            var blockedMinutes = blocked.Where(x => Overlaps(x.StartUtc, x.EndUtc, dayStart, dayEnd))
                .Sum(x => OverlapMinutes(x.StartUtc, x.EndUtc, dayStart, dayEnd));
            var conflicts = events.Where(x => x.Conflict is not null && Overlaps(x.StartUtc, x.EndUtc, dayStart, dayEnd))
                .Select(x => x.Conflict!).Distinct().ToArray();
            var staffedWorkspaces = staff.Where(member => AvailabilityWindowCalculator.ForDate(date,
                weekly.Where(x => x.StaffMemberId == member.Id), exceptions.Where(x => x.StaffMemberId == member.Id)).Any())
                .Select(x => string.IsNullOrWhiteSpace(x.WorkspaceName) ? x.Id.ToString() : x.WorkspaceName)
                .Distinct().Count();
            days.Add(new(date, scheduledMinutes, bookedMinutes, blockedMinutes,
                scheduledMinutes == 0 ? 0 : Math.Round((decimal)bookedMinutes * 100 / scheduledMinutes, 1), staffedWorkspaces, conflicts));
        }
        return new(from, to, zoneId, staff, events, days);
    }
    public async Task<IReadOnlyCollection<SlotDto>> SlotsAsync(Guid staffId, Guid offeringId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (to < from || to.DayNumber - from.DayNumber > 31) return [];
        var offering = await db.ServiceOfferings.Include(x => x.BusinessLocation).SingleOrDefaultAsync(x => x.Id == offeringId && x.StaffMemberId == staffId && x.IsActive && x.Service.IsActive && x.StaffMember.IsActive && x.StaffMember.AcceptingBookings && x.BusinessLocation.IsActive && x.Service.Business.ApprovalStatus == BusinessApprovalStatus.Approved, ct); if (offering is null) return [];
        var weekly = await db.WeeklyAvailabilities.Where(x => x.StaffMemberId == staffId && x.IsActive).ToArrayAsync(ct);
        var exceptions = await db.AvailabilityExceptions.Where(x => x.StaffMemberId == staffId && x.Date >= from && x.Date <= to).ToArrayAsync(ct);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(offering.BusinessLocation.TimeZone); var slots = new List<SlotDto>(); var step = offering.DurationMinutes + offering.BufferMinutes;
        for (var date = from; date <= to; date = date.AddDays(1)) foreach (var window in AvailabilityWindowCalculator.ForDate(date, weekly, exceptions))
            for (var time = window.Start; time.AddMinutes(offering.DurationMinutes) <= window.End; time = time.AddMinutes(step)) { var localStart = date.ToDateTime(time, DateTimeKind.Unspecified); var start = TimeZoneInfo.ConvertTimeToUtc(localStart, zone); var end = start.AddMinutes(offering.DurationMinutes); var blocked = await db.BlockedTimes.AnyAsync(x => x.StaffMemberId == staffId && x.StartUtc < end && x.EndUtc > start, ct) || await db.Bookings.AnyAsync(x => x.StaffMemberId == staffId && (x.Status == BookingStatus.Pending || x.Status == BookingStatus.Confirmed) && x.StartUtc < end && x.EndUtc > start, ct); if (!blocked && start > DateTime.UtcNow) slots.Add(new(start, end)); }
        return slots;
    }
    private Task<bool> OwnsStaff(Guid ownerId, Guid staffId, CancellationToken ct) => db.StaffMembers.AnyAsync(x => x.Id == staffId && x.Business.OwnerUserId == ownerId, ct);
    private static bool Overlaps(DateTime start, DateTime end, DateTime otherStart, DateTime otherEnd) => start < otherEnd && end > otherStart;
    private static int OverlapMinutes(DateTime start, DateTime end, DateTime rangeStart, DateTime rangeEnd)
        => Math.Max(0, (int)(new[] { end, rangeEnd }.Min() - new[] { start, rangeStart }.Max()).TotalMinutes);
    private static bool HasOverlap(IEnumerable<WeeklyAvailabilityRequest> rows) => rows.GroupBy(x => x.DayOfWeek).Any(g => g.OrderBy(x => x.StartTime).Zip(g.OrderBy(x => x.StartTime).Skip(1), (a, b) => a.EndTime > b.StartTime).Any(x => x));
}
