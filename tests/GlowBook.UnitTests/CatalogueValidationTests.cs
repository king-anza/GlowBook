using System.ComponentModel.DataAnnotations;
using GlowBook.Application.Catalogue;

namespace GlowBook.UnitTests;

public sealed class CatalogueValidationTests
{
    [Theory]
    [InlineData(4, 0)]
    [InlineData(30, 181)]
    public void OfferingOutsideAllowedRangesIsInvalid(int duration, int buffer)
    {
        var request = new OfferingRequest(Guid.NewGuid(), 100m, duration, buffer, null);
        var results = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), results, true));
    }
}
