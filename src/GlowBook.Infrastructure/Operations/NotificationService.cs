using System.Security.Cryptography;
using System.Text;
using GlowBook.Application.Operations;
using GlowBook.Domain.Operations;
using GlowBook.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace GlowBook.Infrastructure.Operations;

internal sealed class NotificationService(GlowBookDbContext db, IDataProtectionProvider protectionProvider) : INotificationService
{
    private readonly IDataProtector protector = protectionProvider.CreateProtector("GlowBook.PushDeviceTokens.v1");

    public async Task<NotificationPage> ListAsync(Guid userId, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 50);
        var query = db.Notifications.AsNoTracking().Where(x => x.RecipientUserId == userId);
        var unread = await query.CountAsync(x => x.ReadAtUtc == null, ct);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new NotificationDto(x.Id, x.EventType, x.Title, x.Message, x.ActionUrl, x.CreatedAtUtc, x.ReadAtUtc)).ToArrayAsync(ct);
        return new(items, unread);
    }

    public async Task<bool> MarkReadAsync(Guid userId, Guid id, CancellationToken ct) =>
        await db.Notifications.Where(x => x.Id == id && x.RecipientUserId == userId && x.ReadAtUtc == null)
            .ExecuteUpdateAsync(x => x.SetProperty(n => n.ReadAtUtc, DateTime.UtcNow), ct) == 1;

    public Task MarkAllReadAsync(Guid userId, CancellationToken ct) => db.Notifications
        .Where(x => x.RecipientUserId == userId && x.ReadAtUtc == null)
        .ExecuteUpdateAsync(x => x.SetProperty(n => n.ReadAtUtc, DateTime.UtcNow), ct);

    public async Task<NotificationPreferenceDto> PreferencesAsync(Guid userId, CancellationToken ct)
    {
        var item = await db.NotificationPreferences.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        var available = await db.PushDevices.AnyAsync(x => x.UserId == userId && x.RevokedAtUtc == null, ct);
        return Map(item, available);
    }

    public async Task<NotificationPreferenceDto> UpdatePreferencesAsync(Guid userId, UpdateNotificationPreferenceRequest r, CancellationToken ct)
    {
        var item = await db.NotificationPreferences.SingleOrDefaultAsync(x => x.UserId == userId, ct) ?? new NotificationPreference { UserId = userId };
        if (db.Entry(item).State == EntityState.Detached) db.NotificationPreferences.Add(item);
        item.BookingPush = r.BookingPush; item.ApplicationPush = r.ApplicationPush; item.AccountPush = r.AccountPush;
        item.ReminderPush = r.ReminderPush; item.EmailFallback = r.EmailFallback; item.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(item, await db.PushDevices.AnyAsync(x => x.UserId == userId && x.RevokedAtUtc == null, ct));
    }

    public async Task<PushDeviceDto> RegisterDeviceAsync(Guid userId, RegisterPushDeviceRequest r, CancellationToken ct)
    {
        var token = r.Token.Trim();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        var item = await db.PushDevices.SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (item is null) { item = new PushDevice { Id = Guid.NewGuid(), UserId = userId, TokenHash = hash, CreatedAtUtc = DateTime.UtcNow }; db.PushDevices.Add(item); }
        item.UserId = userId; item.ProtectedToken = protector.Protect(token); item.Platform = r.Platform; item.LastSeenAtUtc = DateTime.UtcNow; item.RevokedAtUtc = null;
        await db.SaveChangesAsync(ct);
        return new(item.Id, item.Platform, item.LastSeenAtUtc);
    }

    public async Task<bool> RevokeDeviceAsync(Guid userId, Guid id, CancellationToken ct) =>
        await db.PushDevices.Where(x => x.Id == id && x.UserId == userId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(x => x.SetProperty(d => d.RevokedAtUtc, DateTime.UtcNow).SetProperty(d => d.ProtectedToken, string.Empty), ct) == 1;

    private static NotificationPreferenceDto Map(NotificationPreference? x, bool available) => new(
        x?.BookingPush ?? true, x?.ApplicationPush ?? true, x?.AccountPush ?? true, x?.ReminderPush ?? true,
        x?.EmailFallback ?? true, available);
}
