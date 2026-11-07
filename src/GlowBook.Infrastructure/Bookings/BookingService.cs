using System.Data;
using GlowBook.Application.Availability;
using GlowBook.Application.Bookings;
using GlowBook.Domain.Bookings;
using GlowBook.Domain.Professionals;
using GlowBook.Domain.Operations;
using GlowBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GlowBook.Infrastructure.Bookings;

internal sealed class BookingService(GlowBookDbContext db, IAvailabilityService availability) : IBookingService
{
    private static readonly BookingStatus[] Occupying = [BookingStatus.Pending, BookingStatus.Confirmed];
    public async Task<BookingDto?> CreateAsync(Guid customerId, CreateBookingRequest request, CancellationToken ct)
    {
        var idempotencyKey = request.IdempotencyKey.Trim();
        if (idempotencyKey.Length == 0) return null;
        if (!await db.Users.AnyAsync(x => x.Id == customerId && x.EmailConfirmed && x.AccountStatus == "Active", ct)) return null;
        var existing = await Query().SingleOrDefaultAsync(x => x.CustomerUserId == customerId && x.IdempotencyKey == idempotencyKey, ct); if (existing is not null) return Map(existing);
        var offering = await db.ServiceOfferings.Include(x => x.Service).ThenInclude(x => x.Business).Include(x => x.StaffMember).Include(x => x.BusinessLocation).SingleOrDefaultAsync(x => x.Id == request.OfferingId && x.IsActive && x.Service.IsActive && x.StaffMember.IsActive && x.StaffMember.AcceptingBookings && x.BusinessLocation.IsActive && x.Service.Business.ApprovalStatus == BusinessApprovalStatus.Approved, ct); if (offering is null) return null;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({offering.StaffMemberId.ToString()}))", ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({$"{customerId}:{idempotencyKey}"}))", ct);
        var start = request.StartUtc.UtcDateTime; var end = start.AddMinutes(offering.DurationMinutes + offering.BufferMinutes);
        existing = await Query().SingleOrDefaultAsync(x => x.CustomerUserId == customerId && x.IdempotencyKey == idempotencyKey, ct); if (existing is not null) return Map(existing);
        if (start <= DateTime.UtcNow) return null;
        if (await db.Bookings.AnyAsync(x => x.StaffMemberId == offering.StaffMemberId && Occupying.Contains(x.Status) && x.StartUtc < end && x.EndUtc > start, ct)) return null;
        var zone = TimeZoneInfo.FindSystemTimeZoneById(offering.BusinessLocation.TimeZone); var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(start, zone));
        var slots = await availability.SlotsAsync(offering.StaffMemberId, offering.Id, localDate, localDate, ct); if (!slots.Any(x => x.StartUtc == start)) return null;
        var booking = new Booking { Id = Guid.NewGuid(), CustomerUserId = customerId, BusinessId = offering.Service.BusinessId, BusinessLocationId = offering.BusinessLocationId, StaffMemberId = offering.StaffMemberId, ServiceOfferingId = offering.Id, StartUtc = start, EndUtc = end, ServiceNameSnapshot = offering.Service.Name, PriceSnapshot = offering.Price, DurationMinutesSnapshot = offering.DurationMinutes, CustomerNotes = request.Notes?.Trim(), IdempotencyKey = idempotencyKey, CreatedAtUtc = DateTime.UtcNow };
        booking.StatusHistory.Add(new BookingStatusHistory { Id = Guid.NewGuid(), NewStatus = BookingStatus.Pending, ActorUserId = customerId, ChangedAtUtc = DateTime.UtcNow });
        db.Bookings.Add(booking);
        db.Notifications.AddRange(Notification(customerId, "BookingCreated", booking.Id), Notification(offering.Service.Business.OwnerUserId, "BookingCreated", booking.Id));
        db.AuditLogs.Add(Audit(customerId, "BookingCreated", booking.Id, null));
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return Map(await Query().SingleAsync(x => x.Id == booking.Id, ct));
    }
    public async Task<IReadOnlyCollection<BookingDto>> CustomerBookingsAsync(Guid id, CancellationToken ct) => (await Query().Where(x => x.CustomerUserId == id).OrderByDescending(x => x.StartUtc).Take(500).ToArrayAsync(ct)).Select(Map).ToArray();
    public async Task<IReadOnlyCollection<BookingDto>> ProfessionalBookingsAsync(Guid id, CancellationToken ct) => (await Query().Where(x => x.Business.OwnerUserId == id || x.StaffMember.LinkedUserId == id).OrderByDescending(x => x.StartUtc).Take(500).ToArrayAsync(ct)).Select(Map).ToArray();
    public async Task<IReadOnlyCollection<BookingDto>> AdminBookingsAsync(CancellationToken ct) => (await Query().OrderByDescending(x => x.StartUtc).Take(500).ToArrayAsync(ct)).Select(Map).ToArray();
    public async Task<bool> TransitionAsync(Guid actorId, bool professional, Guid id, BookingStatus target, BookingActionRequest request, CancellationToken ct)
    {
        var booking = await Query().SingleOrDefaultAsync(x => x.Id == id, ct); if (booking is null || booking.ConcurrencyToken != request.ConcurrencyToken) return false;
        if (professional ? booking.Business.OwnerUserId != actorId && booking.StaffMember.LinkedUserId != actorId : booking.CustomerUserId != actorId) return false;
        var allowed = professional ? AllowedProfessional(booking.Status, target) : target == BookingStatus.Cancelled && booking.Status is BookingStatus.Pending or BookingStatus.Confirmed;
        if (!allowed) return false; var previous = booking.Status; booking.Status = target; booking.ConcurrencyToken = Guid.NewGuid(); if (target == BookingStatus.Confirmed) booking.ConfirmedAtUtc = DateTime.UtcNow; if (target == BookingStatus.Completed) booking.CompletedAtUtc = DateTime.UtcNow;
        booking.StatusHistory.Add(new BookingStatusHistory { Id = Guid.NewGuid(), PreviousStatus = previous, NewStatus = target, ActorUserId = actorId, Reason = request.Reason?.Trim(), ChangedAtUtc = DateTime.UtcNow });
        db.Notifications.Add(Notification(professional ? booking.CustomerUserId : booking.Business.OwnerUserId, $"Booking{target}", booking.Id));
        db.AuditLogs.Add(Audit(actorId, $"Booking{target}", booking.Id, request.Reason?.Trim()));
        await db.SaveChangesAsync(ct); return true;
    }
    private static bool AllowedProfessional(BookingStatus current, BookingStatus target) => (current, target) switch { (BookingStatus.Pending, BookingStatus.Confirmed or BookingStatus.Rejected or BookingStatus.Cancelled) => true, (BookingStatus.Confirmed, BookingStatus.Completed or BookingStatus.NoShow or BookingStatus.Cancelled) => true, _ => false };
    private IQueryable<Booking> Query() => db.Bookings.Include(x => x.Business).Include(x => x.StaffMember).Include(x => x.StatusHistory);
    private static BookingDto Map(Booking x) => new(x.Id, x.CustomerUserId, x.BusinessId, x.StaffMemberId, x.StaffMember.DisplayName, x.Business.TradingName, x.ServiceNameSnapshot, x.PriceSnapshot, x.DurationMinutesSnapshot, x.StartUtc, x.EndUtc, x.Status, x.ConcurrencyToken);
    private static Notification Notification(Guid recipientId, string eventType, Guid bookingId) => new() { Id = Guid.NewGuid(), RecipientUserId = recipientId, EventType = eventType, SubjectType = "Booking", SubjectId = bookingId, CreatedAtUtc = DateTime.UtcNow };
    private static AuditLog Audit(Guid actorId, string action, Guid bookingId, string? details) => new() { Id = Guid.NewGuid(), ActorUserId = actorId, Action = action, SubjectType = "Booking", SubjectId = bookingId, Details = details, OccurredAtUtc = DateTime.UtcNow };
}
