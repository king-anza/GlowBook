using GlowBook.Application.Identity;

namespace GlowBook.Infrastructure.Identity;

internal sealed class SmtpIdentityEmailSender(IEmailClient client) : IIdentityEmailSender
{
    public async Task SendVerificationAsync(string email, Guid userId, string token, CancellationToken cancellationToken) =>
        await client.SendAsync(email, "Verify your GlowBook email", $"Use this user ID and verification token in GlowBook:\n\nUser ID: {userId}\nToken: {token}", cancellationToken);

    public async Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken) =>
        await client.SendAsync(email, "Reset your GlowBook password", $"Use this password-reset token in GlowBook:\n\n{token}", cancellationToken);
}
