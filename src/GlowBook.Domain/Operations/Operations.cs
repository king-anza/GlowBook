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
