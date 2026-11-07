using System.ComponentModel.DataAnnotations;
using GlowBook.Application.Reviews;

namespace GlowBook.UnitTests;

public sealed class ReviewValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Rating_outside_fixed_scale_is_invalid(int rating)
    {
        var request = new ReviewRequest(rating, null);
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }

    [Fact]
    public void Comment_over_limit_is_invalid()
    {
        var request = new ReviewRequest(5, new string('x', 1001));
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }
}
