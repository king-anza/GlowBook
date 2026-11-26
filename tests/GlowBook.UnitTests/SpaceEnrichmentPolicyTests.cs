using GlowBook.Application.Spaces;

namespace GlowBook.UnitTests;

public sealed class SpaceEnrichmentPolicyTests
{
    private static readonly DateOnly Today = new(2026, 11, 26);

    [Fact]
    public void Expiry_and_urgency_are_derived_from_listing_dates()
    {
        Assert.True(SpaceEnrichmentPolicy.IsExpired(Today.AddDays(-1), Today));
        Assert.False(SpaceEnrichmentPolicy.IsUrgent(Today.AddDays(-5), Today.AddDays(-1), Today));
        Assert.True(SpaceEnrichmentPolicy.IsUrgent(Today.AddDays(5), Today.AddDays(30), Today));
        Assert.False(SpaceEnrichmentPolicy.IsUrgent(Today.AddDays(8), Today.AddDays(30), Today));
    }

    [Fact]
    public void Distance_is_rounded_without_exposing_coordinates()
    {
        Assert.Equal(2.1, SpaceEnrichmentPolicy.DistanceKm(-33.9608, 25.6022, -33.9760, 25.5890));
    }
}
