using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GlowBook.Domain.Operations;
using GlowBook.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GlowBook.Infrastructure.Operations;

public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";
    public string ProjectId { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}

internal interface IPushNotificationSender
{
    bool IsConfigured { get; }
    Task<bool> SendAsync(string token, string title, string message, string actionUrl, CancellationToken ct);
}

internal sealed class FirebasePushNotificationSender(HttpClient http, IOptions<FirebaseOptions> options) : IPushNotificationSender
{
    private readonly FirebaseOptions settings = options.Value;
    private readonly SemaphoreSlim tokenLock = new(1, 1);
    private string? accessToken;
    private DateTime accessTokenExpiresAtUtc;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(settings.ProjectId) && !string.IsNullOrWhiteSpace(settings.ClientEmail) && !string.IsNullOrWhiteSpace(settings.PrivateKey);

    public async Task<bool> SendAsync(string token, string title, string message, string actionUrl, CancellationToken ct)
    {
        if (!IsConfigured) return false;
        var bearer = await AccessTokenAsync(ct);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://fcm.googleapis.com/v1/projects/{Uri.EscapeDataString(settings.ProjectId)}/messages:send");
        request.Headers.Authorization = new("Bearer", bearer);
        request.Content = JsonContent.Create(new { message = new { token, notification = new { title, body = message }, data = new { actionUrl } } });
        using var response = await http.SendAsync(request, ct);
        return response.IsSuccessStatusCode;
    }

    private async Task<string> AccessTokenAsync(CancellationToken ct)
    {
        if (accessToken is not null && accessTokenExpiresAtUtc > DateTime.UtcNow.AddMinutes(2)) return accessToken;
        await tokenLock.WaitAsync(ct);
        try
        {
            if (accessToken is not null && accessTokenExpiresAtUtc > DateTime.UtcNow.AddMinutes(2)) return accessToken;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT" }));
            var payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { iss = settings.ClientEmail, scope = "https://www.googleapis.com/auth/firebase.messaging", aud = "https://oauth2.googleapis.com/token", iat = now, exp = now + 3600 }));
            using var rsa = RSA.Create(); rsa.ImportFromPem(settings.PrivateKey.Replace("\\n", "\n"));
            var signature = Base64Url(rsa.SignData(Encoding.UTF8.GetBytes($"{header}.{payload}"), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            using var response = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer", ["assertion"] = $"{header}.{payload}.{signature}" }), ct);
            response.EnsureSuccessStatusCode();
            var token = await response.Content.ReadFromJsonAsync<OAuthToken>(cancellationToken: ct) ?? throw new InvalidOperationException("Firebase OAuth response was empty.");
            accessToken = token.access_token; accessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(token.expires_in); return accessToken;
        }
        finally { tokenLock.Release(); }
    }
    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private sealed record OAuthToken(string access_token, int expires_in);
}

internal sealed class PushNotificationDispatcher(IServiceScopeFactory scopes, IPushNotificationSender sender,
    IDataProtectionProvider protectionProvider, ILogger<PushNotificationDispatcher> logger) : BackgroundService
{
    private readonly IDataProtector protector = protectionProvider.CreateProtector("GlowBook.PushDeviceTokens.v1");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        do { await DispatchAsync(stoppingToken); } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
    private async Task DispatchAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<GlowBookDbContext>();
            var pending = await db.Notifications.Where(x => x.PushProcessedAtUtc == null).OrderBy(x => x.CreatedAtUtc).Take(50).ToArrayAsync(ct);
            foreach (var notification in pending)
            {
                var preference = await db.NotificationPreferences.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == notification.RecipientUserId, ct);
                var enabled = notification.EventType == "BookingReminder" ? preference?.ReminderPush ?? true :
                    notification.EventType.StartsWith("Booking") ? preference?.BookingPush ?? true :
                    notification.EventType.StartsWith("Application") ? preference?.ApplicationPush ?? true : preference?.AccountPush ?? true;
                if (enabled && sender.IsConfigured)
                {
                    var delivered = false;
                    var devices = await db.PushDevices.Where(x => x.UserId == notification.RecipientUserId && x.RevokedAtUtc == null).ToArrayAsync(ct);
                    foreach (var device in devices)
                    {
                        try { delivered |= await sender.SendAsync(protector.Unprotect(device.ProtectedToken), notification.Title, notification.Message, notification.ActionUrl, ct); }
                        catch (Exception exception) { logger.LogWarning(exception, "Push delivery failed for notification {NotificationId}", notification.Id); }
                    }
                    if (delivered) notification.PushDeliveredAtUtc = DateTime.UtcNow;
                }
                notification.PushProcessedAtUtc = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception exception) { logger.LogError(exception, "Push notification dispatch cycle failed"); }
    }
}
