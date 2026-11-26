using System.ComponentModel.DataAnnotations;
using GlowBook.Application.Operations;

namespace GlowBook.UnitTests;

public sealed class NotificationValidationTests
{
    [Theory]
    [InlineData("web")]
    [InlineData("")]
    public void Push_device_rejects_unsupported_platforms(string platform)
    {
        var request = new RegisterPushDeviceRequest("secure-device-token", platform);
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }
}
