namespace GlowBook.Domain.Operations;

public enum NotificationStatus { Pending, Sent, Failed }

public sealed class Notification
{
    public Guid Id { get; set; }
    public Guid RecipientUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
    public DateTime? ReadAtUtc { get; set; }
    public DateTime? PushProcessedAtUtc { get; set; }
    public DateTime? PushDeliveredAtUtc { get; set; }
}

public sealed class NotificationPreference
{
    public Guid UserId { get; set; }
    public bool BookingPush { get; set; } = true;
    public bool ApplicationPush { get; set; } = true;
    public bool AccountPush { get; set; } = true;
    public bool ReminderPush { get; set; } = true;
    public bool EmailFallback { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class PushDevice
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string ProtectedToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}

public sealed class AuditLog
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string? Details { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
