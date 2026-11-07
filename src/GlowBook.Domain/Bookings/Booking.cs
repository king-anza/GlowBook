using GlowBook.Domain.Catalogue;
using GlowBook.Domain.Professionals;

namespace GlowBook.Domain.Bookings;

public enum BookingStatus { Pending, Confirmed, Rejected, Cancelled, Completed, NoShow }

public sealed class Booking
{
    public Guid Id { get; set; }
    public Guid CustomerUserId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid BusinessLocationId { get; set; }
    public Guid StaffMemberId { get; set; }
    public Guid ServiceOfferingId { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public string ServiceNameSnapshot { get; set; } = string.Empty;
    public decimal PriceSnapshot { get; set; }
    public int DurationMinutesSnapshot { get; set; }
    public string? CustomerNotes { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public Business Business { get; set; } = null!;
    public BusinessLocation BusinessLocation { get; set; } = null!;
    public StaffMember StaffMember { get; set; } = null!;
    public ServiceOffering ServiceOffering { get; set; } = null!;
    public ICollection<BookingStatusHistory> StatusHistory { get; set; } = [];
}

public sealed class BookingStatusHistory
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public BookingStatus? PreviousStatus { get; set; }
    public BookingStatus NewStatus { get; set; }
    public Guid ActorUserId { get; set; }
    public string? Reason { get; set; }
    public DateTime ChangedAtUtc { get; set; }
    public Booking Booking { get; set; } = null!;
}
