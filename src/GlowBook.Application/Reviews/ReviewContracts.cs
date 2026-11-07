using System.ComponentModel.DataAnnotations;

namespace GlowBook.Application.Reviews;

public sealed record ReviewRequest(
    [property: Range(1, 5)] int Rating,
    [property: MaxLength(1000)] string? Comment,
    Guid? ConcurrencyToken = null);

public sealed record ReviewDto(Guid Id, Guid BookingId, Guid BusinessId, int Rating, string? Comment, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, Guid ConcurrencyToken);
public sealed record BusinessReviewsDto(double AverageRating, int ReviewCount, IReadOnlyCollection<ReviewDto> Reviews);

public interface IReviewService
{
    Task<ReviewDto?> CreateAsync(Guid customerId, Guid bookingId, ReviewRequest request, CancellationToken ct);
    Task<ReviewDto?> UpdateAsync(Guid customerId, Guid reviewId, ReviewRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid customerId, Guid reviewId, CancellationToken ct);
    Task<IReadOnlyCollection<ReviewDto>> ForCustomerAsync(Guid customerId, CancellationToken ct);
    Task<BusinessReviewsDto> ForBusinessAsync(Guid businessId, int page, int pageSize, CancellationToken ct);
    Task<bool> SetHiddenAsync(Guid actorId, Guid reviewId, bool hidden, CancellationToken ct);
}
