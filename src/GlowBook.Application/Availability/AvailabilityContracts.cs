using System.ComponentModel.DataAnnotations;
using GlowBook.Domain.Availability;

namespace GlowBook.Application.Availability;

public sealed record WeeklyAvailabilityRequest(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);
public sealed record ExceptionRequest(DateOnly Date, TimeOnly? StartTime, TimeOnly? EndTime, AvailabilityExceptionType Type, [property: MaxLength(300)] string? Reason);
public sealed record BlockedTimeRequest(DateTimeOffset StartUtc, DateTimeOffset EndUtc, [property: MaxLength(300)] string? Reason);
public sealed record AvailabilityDto(Guid Id, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);
public sealed record SlotDto(DateTime StartUtc, DateTime EndUtc);

public interface IAvailabilityService
{
    Task<IReadOnlyCollection<AvailabilityDto>> GetWeeklyAsync(Guid ownerId, Guid staffId, CancellationToken ct);
    Task<bool> ReplaceWeeklyAsync(Guid ownerId, Guid staffId, IReadOnlyCollection<WeeklyAvailabilityRequest> requests, CancellationToken ct);
    Task<bool> AddExceptionAsync(Guid ownerId, Guid staffId, ExceptionRequest request, CancellationToken ct);
    Task<bool> AddBlockedTimeAsync(Guid ownerId, Guid staffId, BlockedTimeRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<SlotDto>> SlotsAsync(Guid staffId, Guid offeringId, DateOnly from, DateOnly to, CancellationToken ct);
}
