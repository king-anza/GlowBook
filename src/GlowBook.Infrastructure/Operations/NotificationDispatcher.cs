using GlowBook.Application.Identity;
using GlowBook.Domain.Operations;
using GlowBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GlowBook.Infrastructure.Operations;

internal sealed class NotificationDispatcher(IServiceScopeFactory scopes, IEmailClient email, IPushNotificationSender push, ILogger<NotificationDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        do { await DispatchAsync(stoppingToken); } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task DispatchAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<GlowBookDbContext>();
            var pending = await db.Notifications.Where(x => x.Status == NotificationStatus.Pending && x.AttemptCount < 5).OrderBy(x => x.CreatedAtUtc).Take(20).ToListAsync(ct);
            foreach (var item in pending)
            {
                var recipient = await db.Users.Where(x => x.Id == item.RecipientUserId).Select(x => x.Email).SingleOrDefaultAsync(ct);
                item.AttemptCount++;
                try
                {
                    var preference = await db.NotificationPreferences.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == item.RecipientUserId, ct);
                    var hasPush = push.IsConfigured && await db.PushDevices.AnyAsync(x => x.UserId == item.RecipientUserId && x.RevokedAtUtc == null, ct);
                    if (hasPush && item.PushProcessedAtUtc is null) { item.AttemptCount--; continue; }
                    var shouldEmail = preference is null || preference.EmailFallback && (!hasPush || item.PushDeliveredAtUtc is null);
                    if (!shouldEmail || !string.IsNullOrWhiteSpace(recipient) && await email.SendAsync(recipient, item.Title, $"{item.Message}\n\nOpen Glow by LunaMay to view the details.\n\nFind it. Book it. Glow.", ct))
                    {
                        item.Status = NotificationStatus.Sent;
                        item.ProcessedAtUtc = DateTime.UtcNow;
                    }
                    else if (item.AttemptCount >= 5)
                    {
                        item.Status = NotificationStatus.Failed;
                        item.ProcessedAtUtc = DateTime.UtcNow;
                    }
                }
                catch (Exception exception) { logger.LogWarning(exception, "Notification {NotificationId} delivery attempt failed", item.Id); if (item.AttemptCount >= 5) item.Status = NotificationStatus.Failed; }
            }
            await db.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception exception) { logger.LogError(exception, "Notification dispatch cycle failed"); }
    }
}
