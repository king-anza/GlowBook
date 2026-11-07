using System.Net;
using System.Net.Mail;
using GlowBook.Application.Identity;
using Microsoft.Extensions.Options;

namespace GlowBook.Infrastructure.Identity;

internal sealed class SmtpEmailClient(IOptions<SmtpOptions> options) : IEmailClient
{
    private readonly SmtpOptions options = options.Value;

    public async Task<bool> SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        using var message = new MailMessage { From = new MailAddress(options.FromAddress, options.FromName), Subject = subject, Body = body, IsBodyHtml = false };
        message.To.Add(recipient);
        using var client = new SmtpClient(options.Host, options.Port) { EnableSsl = options.EnableSsl, Credentials = string.IsNullOrWhiteSpace(options.Username) ? CredentialCache.DefaultNetworkCredentials : new NetworkCredential(options.Username, options.Password) };
        await client.SendMailAsync(message, cancellationToken);
        return true;
    }
}
