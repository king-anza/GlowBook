using GlowBook.Application.Professionals;

namespace GlowBook.UnitTests;

public sealed class ProfessionalInsightPolicyTests
{
    [Fact]
    public void Repeat_customer_count_is_suppressed_below_privacy_threshold()
    {
        var customer = Guid.NewGuid();

        var result = ProfessionalInsightPolicy.RepeatCustomerCount([customer, customer], 2);

        Assert.Null(result);
    }

    [Fact]
    public void Repeat_customer_count_and_utilisation_are_aggregated()
    {
        var repeat = Guid.NewGuid();
        var single = Guid.NewGuid();

        Assert.Equal(1, ProfessionalInsightPolicy.RepeatCustomerCount([repeat, repeat, single], 3));
        Assert.Equal(25m, ProfessionalInsightPolicy.UtilisationPercent(120, 480));
        Assert.Equal(0m, ProfessionalInsightPolicy.UtilisationPercent(120, 0));
    }
}
