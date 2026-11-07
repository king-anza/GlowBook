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
    public async Task<bool> AddBlockedTimeAsync(Guid ownerId, Guid staffId, BlockedTimeRequest request, CancellationToken ct) { var start = request.StartUtc.UtcDateTime; var end = request.EndUtc.UtcDateTime; if (!await OwnsStaff(ownerId, staffId, ct) || start >= end || end - start > TimeSpan.FromDays(31)) return false; db.BlockedTimes.Add(new BlockedTime { Id = Guid.NewGuid(), StaffMemberId = staffId, StartUtc = start, EndUtc = end, Reason = request.Reason?.Trim(), CreatedAtUtc = DateTime.UtcNow }); await db.SaveChangesAsync(ct); return true; }
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
    private static bool HasOverlap(IEnumerable<WeeklyAvailabilityRequest> rows) => rows.GroupBy(x => x.DayOfWeek).Any(g => g.OrderBy(x => x.StartTime).Zip(g.OrderBy(x => x.StartTime).Skip(1), (a, b) => a.EndTime > b.StartTime).Any(x => x));
}
