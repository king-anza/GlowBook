using System.ComponentModel.DataAnnotations;
using GlowBook.Domain.Bookings;

namespace GlowBook.Application.Bookings;

public sealed record CreateBookingRequest(Guid OfferingId, DateTimeOffset StartUtc, [property: Required, MaxLength(100)] string IdempotencyKey, [property: MaxLength(500)] string? Notes);
public sealed record BookingActionRequest([property: MaxLength(500)] string? Reason, Guid ConcurrencyToken);
public sealed record BookingDto(Guid Id, Guid CustomerUserId, Guid BusinessId, Guid StaffMemberId, string StaffName, string BusinessName, string ServiceName, decimal Price, int DurationMinutes, DateTime StartUtc, DateTime EndUtc, BookingStatus Status, Guid ConcurrencyToken);

public interface IBookingService
{
    Task<BookingDto?> CreateAsync(Guid customerId, CreateBookingRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<BookingDto>> CustomerBookingsAsync(Guid customerId, CancellationToken ct);
    Task<IReadOnlyCollection<BookingDto>> ProfessionalBookingsAsync(Guid ownerId, CancellationToken ct);
    Task<IReadOnlyCollection<BookingDto>> AdminBookingsAsync(CancellationToken ct);
    Task<bool> TransitionAsync(Guid actorId, bool professional, Guid bookingId, BookingStatus target, BookingActionRequest request, CancellationToken ct);
}
