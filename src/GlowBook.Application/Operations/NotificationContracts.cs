using System.ComponentModel.DataAnnotations;

namespace GlowBook.Application.Operations;

public sealed record NotificationDto(Guid Id, string EventType, string Title, string Message, string ActionUrl,
    DateTime CreatedAtUtc, DateTime? ReadAtUtc);
public sealed record NotificationPage(IReadOnlyCollection<NotificationDto> Items, int UnreadCount);
public sealed record NotificationPreferenceDto(bool BookingPush, bool ApplicationPush, bool AccountPush,
    bool ReminderPush, bool EmailFallback, bool PushAvailable);
public sealed record UpdateNotificationPreferenceRequest(bool BookingPush, bool ApplicationPush, bool AccountPush,
    bool ReminderPush, bool EmailFallback);
public sealed record RegisterPushDeviceRequest([property: Required, MaxLength(4096)] string Token,
    [property: Required, RegularExpression("^(android|ios)$")] string Platform);
public sealed record PushDeviceDto(Guid Id, string Platform, DateTime LastSeenAtUtc);

public interface INotificationService
{
    Task<NotificationPage> ListAsync(Guid userId, int page, int pageSize, CancellationToken ct);
    Task<bool> MarkReadAsync(Guid userId, Guid id, CancellationToken ct);
    Task MarkAllReadAsync(Guid userId, CancellationToken ct);
    Task<NotificationPreferenceDto> PreferencesAsync(Guid userId, CancellationToken ct);
    Task<NotificationPreferenceDto> UpdatePreferencesAsync(Guid userId, UpdateNotificationPreferenceRequest request, CancellationToken ct);
    Task<PushDeviceDto> RegisterDeviceAsync(Guid userId, RegisterPushDeviceRequest request, CancellationToken ct);
    Task<bool> RevokeDeviceAsync(Guid userId, Guid id, CancellationToken ct);
}
