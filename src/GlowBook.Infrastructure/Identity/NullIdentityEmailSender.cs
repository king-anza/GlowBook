using GlowBook.Application.Identity;

namespace GlowBook.Infrastructure.Identity;

internal sealed class NullIdentityEmailSender : IIdentityEmailSender
{
    public Task SendVerificationAsync(string email, Guid userId, string token, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken) => Task.CompletedTask;
}
