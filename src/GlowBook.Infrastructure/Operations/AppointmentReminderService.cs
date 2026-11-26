using GlowBook.Domain.Bookings;
using GlowBook.Domain.Operations;
using GlowBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GlowBook.Infrastructure.Operations;

internal sealed class AppointmentReminderService(IServiceScopeFactory scopes, ILogger<AppointmentReminderService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        do { await CreateRemindersAsync(stoppingToken); } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
    private async Task CreateRemindersAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<GlowBookDbContext>();
            var from = DateTime.UtcNow.AddHours(23); var to = DateTime.UtcNow.AddHours(25);
            var bookings = await db.Bookings.AsNoTracking().Where(x => x.Status == BookingStatus.Confirmed && x.StartUtc >= from && x.StartUtc <= to &&
                !db.Notifications.Any(n => n.EventType == "BookingReminder" && n.SubjectId == x.Id && n.RecipientUserId == x.CustomerUserId))
                .Select(x => new { x.Id, x.CustomerUserId, x.ServiceNameSnapshot }).Take(100).ToArrayAsync(ct);
            foreach (var booking in bookings) db.Notifications.Add(new Notification { Id = Guid.NewGuid(), RecipientUserId = booking.CustomerUserId,
                EventType = "BookingReminder", Title = "Appointment tomorrow", Message = $"Your {booking.ServiceNameSnapshot} appointment is coming up in about 24 hours.",
                ActionUrl = $"/bookings?booking={booking.Id}", SubjectType = "Booking", SubjectId = booking.Id, CreatedAtUtc = DateTime.UtcNow });
            if (bookings.Length > 0) await db.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception exception) { logger.LogError(exception, "Appointment reminder generation failed"); }
    }
}
