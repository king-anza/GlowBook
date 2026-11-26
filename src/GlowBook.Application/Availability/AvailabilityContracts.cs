using System.ComponentModel.DataAnnotations;
using GlowBook.Domain.Availability;

namespace GlowBook.Application.Availability;

public sealed record WeeklyAvailabilityRequest(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);
public sealed record ExceptionRequest(DateOnly Date, TimeOnly? StartTime, TimeOnly? EndTime, AvailabilityExceptionType Type, [property: MaxLength(300)] string? Reason);
public sealed record BlockedTimeRequest(DateTimeOffset StartUtc, DateTimeOffset EndUtc, [property: MaxLength(300)] string? Reason);
public sealed record AvailabilityDto(Guid Id, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);
public sealed record SlotDto(DateTime StartUtc, DateTime EndUtc);
public sealed record ProfessionalCalendarStaffDto(Guid Id, string DisplayName, string? WorkspaceName);
public sealed record ProfessionalCalendarEventDto(Guid Id, string Type, string Title, Guid StaffId, string StaffName,
    string? WorkspaceName, DateTime StartUtc, DateTime EndUtc, string Status, bool CanDelete, string? Conflict);
public sealed record ProfessionalCalendarDayDto(DateOnly Date, int ScheduledMinutes, int BookedMinutes,
    int BlockedMinutes, decimal UtilisationPercent, int StaffedWorkspaceCount, IReadOnlyCollection<string> Conflicts);
public sealed record ProfessionalCalendarDto(DateOnly From, DateOnly To, string TimeZone,
    IReadOnlyCollection<ProfessionalCalendarStaffDto> Staff,
    IReadOnlyCollection<ProfessionalCalendarEventDto> Events,
    IReadOnlyCollection<ProfessionalCalendarDayDto> Days);
public enum BlockedTimeResult { Added, NotOwned, InvalidRange, BookingConflict }

public interface IAvailabilityService
{
    Task<IReadOnlyCollection<AvailabilityDto>> GetWeeklyAsync(Guid ownerId, Guid staffId, CancellationToken ct);
    Task<bool> ReplaceWeeklyAsync(Guid ownerId, Guid staffId, IReadOnlyCollection<WeeklyAvailabilityRequest> requests, CancellationToken ct);
    Task<bool> AddExceptionAsync(Guid ownerId, Guid staffId, ExceptionRequest request, CancellationToken ct);
    Task<bool> AddBlockedTimeAsync(Guid ownerId, Guid staffId, BlockedTimeRequest request, CancellationToken ct);
    Task<BlockedTimeResult> AddBlockedTimeWithConflictCheckAsync(Guid ownerId, Guid staffId, BlockedTimeRequest request, CancellationToken ct);
    Task<bool> DeleteBlockedTimeAsync(Guid ownerId, Guid blockedTimeId, CancellationToken ct);
    Task<ProfessionalCalendarDto?> CalendarAsync(Guid ownerId, DateOnly from, DateOnly to, Guid? staffId, CancellationToken ct);
    Task<IReadOnlyCollection<SlotDto>> SlotsAsync(Guid staffId, Guid offeringId, DateOnly from, DateOnly to, CancellationToken ct);
}
