using GlowBook.Domain.Professionals;

namespace GlowBook.Domain.Availability;

public enum AvailabilityExceptionType { Unavailable, AddedAvailability }

public sealed class WeeklyAvailability
{
    public Guid Id { get; set; }
    public Guid StaffMemberId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsActive { get; set; } = true;
    public StaffMember StaffMember { get; set; } = null!;
}

public sealed class AvailabilityException
{
    public Guid Id { get; set; }
    public Guid StaffMemberId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public AvailabilityExceptionType Type { get; set; }
    public string? Reason { get; set; }
    public StaffMember StaffMember { get; set; } = null!;
}

public sealed class BlockedTime
{
    public Guid Id { get; set; }
    public Guid StaffMemberId { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public StaffMember StaffMember { get; set; } = null!;
}

public readonly record struct AvailabilityWindow(TimeOnly Start, TimeOnly End);

public static class AvailabilityWindowCalculator
{
    public static IReadOnlyCollection<AvailabilityWindow> ForDate(
        DateOnly date,
        IEnumerable<WeeklyAvailability> weekly,
        IEnumerable<AvailabilityException> exceptions)
    {
        var dayExceptions = exceptions.Where(x => x.Date == date).ToArray();
        if (dayExceptions.Any(x => x.Type == AvailabilityExceptionType.Unavailable && x.StartTime is null && x.EndTime is null))
            return [];

        var windows = weekly
            .Where(x => x.IsActive && x.DayOfWeek == date.DayOfWeek)
            .Select(x => new AvailabilityWindow(x.StartTime, x.EndTime))
            .Concat(dayExceptions
                .Where(x => x.Type == AvailabilityExceptionType.AddedAvailability && x.StartTime.HasValue && x.EndTime.HasValue)
                .Select(x => new AvailabilityWindow(x.StartTime!.Value, x.EndTime!.Value)))
            .ToList();

        foreach (var unavailable in dayExceptions.Where(x =>
                     x.Type == AvailabilityExceptionType.Unavailable && x.StartTime.HasValue && x.EndTime.HasValue))
        {
            var cutStart = unavailable.StartTime!.Value;
            var cutEnd = unavailable.EndTime!.Value;
            windows = windows.SelectMany(window => Subtract(window, cutStart, cutEnd)).ToList();
        }

        return Merge(windows);
    }

    private static IEnumerable<AvailabilityWindow> Subtract(AvailabilityWindow window, TimeOnly cutStart, TimeOnly cutEnd)
    {
        if (cutEnd <= window.Start || cutStart >= window.End)
        {
            yield return window;
            yield break;
        }

        if (cutStart > window.Start) yield return new(window.Start, cutStart);
        if (cutEnd < window.End) yield return new(cutEnd, window.End);
    }

    private static IReadOnlyCollection<AvailabilityWindow> Merge(IEnumerable<AvailabilityWindow> windows)
    {
        var ordered = windows.Where(x => x.Start < x.End).OrderBy(x => x.Start).ThenBy(x => x.End).ToArray();
        if (ordered.Length == 0) return [];
        var merged = new List<AvailabilityWindow> { ordered[0] };
        foreach (var next in ordered.Skip(1))
        {
            var current = merged[^1];
            if (next.Start <= current.End)
                merged[^1] = current with { End = next.End > current.End ? next.End : current.End };
            else
                merged.Add(next);
        }
        return merged;
    }
}
