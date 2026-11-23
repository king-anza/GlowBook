using GlowBook.Application.Identity;
using GlowBook.Domain.Operations;
using GlowBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GlowBook.Infrastructure.Operations;

internal sealed class NotificationDispatcher(IServiceScopeFactory scopes, IEmailClient email, ILogger<NotificationDispatcher> logger) : BackgroundService
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
                    if (!string.IsNullOrWhiteSpace(recipient) && await email.SendAsync(recipient, "GlowBook booking update", $"Booking event: {item.EventType}\nBooking reference: {item.SubjectId}", ct))
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
