using GlowBook.Domain.Bookings;
using GlowBook.Domain.Professionals;

namespace GlowBook.Domain.Reviews;

public enum ReviewModerationStatus { Active, Hidden }

public sealed class Review
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid CustomerUserId { get; set; }
    public Guid BusinessId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public ReviewModerationStatus ModerationStatus { get; set; } = ReviewModerationStatus.Active;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public Booking Booking { get; set; } = null!;
    public Business Business { get; set; } = null!;
}
