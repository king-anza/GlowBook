using GlowBook.Application.Spaces;
using GlowBook.Domain.Spaces;

namespace GlowBook.UnitTests;

public sealed class SpaceMatchingPolicyTests
{
    [Fact]
    public void Evaluate_explains_skill_location_type_and_availability_matches()
    {
        var result = SpaceMatchingPolicy.Evaluate("Braiding, Colour", "Thohoyandou", new DateOnly(2026, 12, 1), null,
            OpportunityType.ChairRental, "Braiding, Nails", ["Thohoyandou"], new DateOnly(2026, 11, 30), [OpportunityType.ChairRental]);

        Assert.Equal(85, result.Score);
        Assert.Contains("1 requested skill match", result.Reasons);
        Assert.Contains("In your preferred city, Thohoyandou", result.Reasons);
        Assert.Contains("Matches your preferred opportunity type", result.Reasons);
        Assert.Contains("Fits your availability date", result.Reasons);
    }

    [Fact]
    public void Evaluate_does_not_claim_unmatched_preferences()
    {
        var result = SpaceMatchingPolicy.Evaluate("Massage", "Polokwane", new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 2),
            OpportunityType.StaffVacancy, "Nails", ["Thohoyandou"], new DateOnly(2027, 1, 1), [OpportunityType.ChairRental]);

        Assert.Equal(0, result.Score);
        Assert.Empty(result.Reasons);
    }
}
