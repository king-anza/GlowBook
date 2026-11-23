using System.ComponentModel.DataAnnotations;
using GlowBook.Application.Accounts;
using GlowBook.Application.Bookings;
using GlowBook.Application.Identity;

namespace GlowBook.UnitTests;

public sealed class InputValidationTests
{
    [Fact]
    public void Registration_rejects_invalid_email_short_password_and_missing_name()
    {
        var request = new RegisterRequest("not-an-email", "short", "", true, true);

        var errors = Validate(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(RegisterRequest.Email)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(RegisterRequest.Password)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(RegisterRequest.DisplayName)));
    }

    [Fact]
    public void Booking_rejects_missing_idempotency_key_and_overlong_notes()
    {
        var request = new CreateBookingRequest(Guid.NewGuid(), DateTimeOffset.UtcNow, "", new string('x', 501));

        var errors = Validate(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateBookingRequest.IdempotencyKey)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateBookingRequest.Notes)));
    }

    [Fact]
    public void Account_update_rejects_invalid_email_and_missing_display_name()
    {
        var errors = Validate(new UpdateAccountRequest("invalid", "", false));

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(UpdateAccountRequest.Email)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(UpdateAccountRequest.DisplayName)));
    }

    private static IReadOnlyCollection<ValidationResult> Validate(object value)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(value, new ValidationContext(value), results, true);
        return results;
    }
}
