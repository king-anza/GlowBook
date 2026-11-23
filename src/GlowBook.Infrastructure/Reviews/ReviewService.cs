using GlowBook.Application.Reviews;
using GlowBook.Domain.Bookings;
using GlowBook.Domain.Reviews;
using GlowBook.Domain.Operations;
using GlowBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using GlowBook.Domain.Professionals;

namespace GlowBook.Infrastructure.Reviews;

public sealed class ReviewService(GlowBookDbContext db) : IReviewService
{
    public async Task<ReviewDto?> CreateAsync(Guid customerId, Guid bookingId, ReviewRequest request, CancellationToken ct)
    {
        var booking = await db.Bookings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == bookingId && x.CustomerUserId == customerId, ct);
        if (booking is null || booking.Status != BookingStatus.Completed || await db.Reviews.AnyAsync(x => x.BookingId == bookingId, ct)) return null;
        var now = DateTime.UtcNow;
        var review = new Review { Id = Guid.NewGuid(), BookingId = bookingId, CustomerUserId = customerId, BusinessId = booking.BusinessId, Rating = request.Rating, Comment = Normalise(request.Comment), CreatedAtUtc = now, UpdatedAtUtc = now };
        db.Reviews.Add(review);
        db.AuditLogs.Add(Audit(customerId, "ReviewCreated", review.Id));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { return null; }
        return Map(review);
    }

    public async Task<ReviewDto?> UpdateAsync(Guid customerId, Guid reviewId, ReviewRequest request, CancellationToken ct)
    {
        var review = await db.Reviews.SingleOrDefaultAsync(x => x.Id == reviewId && x.CustomerUserId == customerId, ct);
        if (review is null || request.ConcurrencyToken is null || review.ConcurrencyToken != request.ConcurrencyToken) return null;
        review.Rating = request.Rating; review.Comment = Normalise(request.Comment); review.UpdatedAtUtc = DateTime.UtcNow; review.ConcurrencyToken = Guid.NewGuid();
        db.AuditLogs.Add(Audit(customerId, "ReviewUpdated", review.Id));
        await db.SaveChangesAsync(ct);
        return Map(review);
    }

    public async Task<bool> DeleteAsync(Guid customerId, Guid reviewId, CancellationToken ct)
    {
        var review = await db.Reviews.SingleOrDefaultAsync(x => x.Id == reviewId && x.CustomerUserId == customerId, ct);
        if (review is null) return false;
        db.Reviews.Remove(review); db.AuditLogs.Add(Audit(customerId, "ReviewDeleted", review.Id)); await db.SaveChangesAsync(ct); return true;
    }

    public async Task<IReadOnlyCollection<ReviewDto>> ForCustomerAsync(Guid customerId, CancellationToken ct) =>
        await db.Reviews.AsNoTracking().Where(x => x.CustomerUserId == customerId).OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ReviewDto(x.Id, x.BookingId, x.BusinessId, x.Rating, x.Comment, x.CreatedAtUtc, x.UpdatedAtUtc, x.ConcurrencyToken)).Take(100).ToListAsync(ct);

    public async Task<BusinessReviewsDto> ForBusinessAsync(Guid businessId, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Clamp(page, 1, 10_000); pageSize = Math.Clamp(pageSize, 1, 50);
        var query = db.Reviews.AsNoTracking().Where(x => x.BusinessId == businessId && x.Business.ApprovalStatus == BusinessApprovalStatus.Approved && x.Business.Locations.Any(l => l.IsPrimary && l.IsActive) && x.ModerationStatus == ReviewModerationStatus.Active);
        var count = await query.CountAsync(ct);
        var average = count == 0 ? 0 : await query.AverageAsync(x => x.Rating, ct);
        var reviews = await query.OrderByDescending(x => x.CreatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ReviewDto(x.Id, x.BookingId, x.BusinessId, x.Rating, x.Comment, x.CreatedAtUtc, x.UpdatedAtUtc, x.ConcurrencyToken)).ToListAsync(ct);
        return new BusinessReviewsDto(average, count, reviews);
    }

    public async Task<bool> SetHiddenAsync(Guid actorId, Guid reviewId, bool hidden, CancellationToken ct)
    {
        var review = await db.Reviews.SingleOrDefaultAsync(x => x.Id == reviewId, ct);
        if (review is null) return false;
        review.ModerationStatus = hidden ? ReviewModerationStatus.Hidden : ReviewModerationStatus.Active; review.UpdatedAtUtc = DateTime.UtcNow; review.ConcurrencyToken = Guid.NewGuid();
        db.AuditLogs.Add(Audit(actorId, hidden ? "ReviewHidden" : "ReviewRestored", review.Id));
        await db.SaveChangesAsync(ct); return true;
    }

    public async Task<IReadOnlyCollection<AdminReviewDto>> ForModerationAsync(CancellationToken ct) =>
        await db.Reviews.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(100)
            .Select(x => new AdminReviewDto(x.Id, x.Business.TradingName, x.Rating, x.Comment, x.ModerationStatus.ToString(), x.CreatedAtUtc)).ToListAsync(ct);

    private static string? Normalise(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static ReviewDto Map(Review review) => new(review.Id, review.BookingId, review.BusinessId, review.Rating, review.Comment, review.CreatedAtUtc, review.UpdatedAtUtc, review.ConcurrencyToken);
    private static AuditLog Audit(Guid actorId, string action, Guid reviewId) => new() { Id = Guid.NewGuid(), ActorUserId = actorId, Action = action, SubjectType = "Review", SubjectId = reviewId, OccurredAtUtc = DateTime.UtcNow };
}
