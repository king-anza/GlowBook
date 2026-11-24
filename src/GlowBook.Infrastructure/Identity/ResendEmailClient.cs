using System.Net.Http.Headers;
using System.Net.Http.Json;
using GlowBook.Application.Identity;
using Microsoft.Extensions.Options;

namespace GlowBook.Infrastructure.Identity;

internal sealed class ResendEmailClient(HttpClient httpClient, IOptions<ResendOptions> options) : IEmailClient
{
    private readonly ResendOptions options = options.Value;

    public async Task<bool> SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            from = $"{options.FromName} <{options.FromAddress}>",
            to = new[] { recipient },
            subject,
            text = body
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode) return true;

        var providerResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Resend rejected the email request with status {(int)response.StatusCode}: {providerResponse}",
            null,
            response.StatusCode);
    }
}
