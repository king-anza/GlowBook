using GlowBook.Domain.Availability;

namespace GlowBook.UnitTests;

public sealed class AvailabilityWindowCalculatorTests
{
    private static readonly DateOnly Monday = new(2026, 11, 9);

    [Fact]
    public void Partial_unavailability_splits_a_working_window()
    {
        var weekly = new[] { Weekly(new TimeOnly(9, 0), new TimeOnly(17, 0)) };
        var exceptions = new[] { Exception(AvailabilityExceptionType.Unavailable, new TimeOnly(12, 0), new TimeOnly(13, 0)) };

        var result = AvailabilityWindowCalculator.ForDate(Monday, weekly, exceptions);

        Assert.Equal(
            [new AvailabilityWindow(new TimeOnly(9, 0), new TimeOnly(12, 0)), new AvailabilityWindow(new TimeOnly(13, 0), new TimeOnly(17, 0))],
            result);
    }

    [Fact]
    public void Added_and_weekly_overlaps_are_merged_without_duplicate_time()
    {
        var weekly = new[] { Weekly(new TimeOnly(9, 0), new TimeOnly(12, 0)) };
        var exceptions = new[] { Exception(AvailabilityExceptionType.AddedAvailability, new TimeOnly(11, 0), new TimeOnly(14, 0)) };

        var result = AvailabilityWindowCalculator.ForDate(Monday, weekly, exceptions);

        Assert.Equal([new AvailabilityWindow(new TimeOnly(9, 0), new TimeOnly(14, 0))], result);
    }

    [Fact]
    public void Full_day_unavailability_removes_all_windows()
    {
        var result = AvailabilityWindowCalculator.ForDate(
            Monday,
            new[] { Weekly(new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            new[] { Exception(AvailabilityExceptionType.Unavailable, null, null) });

        Assert.Empty(result);
    }

    private static WeeklyAvailability Weekly(TimeOnly start, TimeOnly end) =>
        new() { DayOfWeek = DayOfWeek.Monday, StartTime = start, EndTime = end, IsActive = true };

    private static AvailabilityException Exception(AvailabilityExceptionType type, TimeOnly? start, TimeOnly? end) =>
        new() { Date = Monday, Type = type, StartTime = start, EndTime = end };
}
