using GlowBook.Application.Identity;
using Microsoft.Extensions.Options;

namespace GlowBook.Infrastructure.Identity;

internal sealed class SmtpIdentityEmailSender(IEmailClient client, IOptions<PublicAppOptions> publicAppOptions) : IIdentityEmailSender
{
    private readonly string baseUrl = publicAppOptions.Value.BaseUrl.TrimEnd('/');

    public async Task SendVerificationAsync(string email, Guid userId, string token, CancellationToken cancellationToken) =>
        await client.SendAsync(
            email,
            "Verify your Glow by LunaMay email",
            $"Welcome to Glow by LunaMay. Verify your email by opening this secure link:\n\n{baseUrl}/auth/recovery?action=verify&userId={userId}&token={Uri.EscapeDataString(token)}\n\nIf you did not create this account, ignore this message.\n\nFind it. Book it. Glow.",
            cancellationToken);

    public async Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken) =>
        await client.SendAsync(
            email,
            "Reset your Glow by LunaMay password",
            $"Reset your Glow by LunaMay password using this secure link:\n\n{baseUrl}/auth/recovery?action=reset&email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}\n\nIf you did not request this reset, ignore this message.\n\nFind it. Book it. Glow.",
            cancellationToken);
}
