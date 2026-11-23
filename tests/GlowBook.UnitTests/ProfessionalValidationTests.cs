using System.ComponentModel.DataAnnotations;
using GlowBook.Application.Professionals;

namespace GlowBook.UnitTests;

public sealed class ProfessionalValidationTests
{
    [Theory]
    [InlineData("http://example.test/image.jpg")]
    [InlineData("not-a-url")]
    public void Portfolio_image_requires_https_url(string url)
    {
        var request = new PortfolioImageRequest(url, "Example style", 0);
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }

    [Fact]
    public void Portfolio_image_accepts_https_url_and_accessible_alt_text()
    {
        var request = new PortfolioImageRequest("https://example.test/image.jpg", "Example style", 9);
        Assert.True(Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }
}
