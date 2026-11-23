namespace GlowBook.Application.Operations;

public sealed record AdminUserDto(Guid Id, string Email, string DisplayName, string AccountStatus, DateTime CreatedAtUtc);
public sealed record AuditLogDto(Guid Id, Guid? ActorUserId, string Action, string SubjectType, Guid SubjectId, string? Details, DateTime OccurredAtUtc);

public interface IOperationsService
{
    Task<IReadOnlyCollection<AdminUserDto>> UsersAsync(int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyCollection<AuditLogDto>> AuditAsync(int page, int pageSize, CancellationToken ct);
}
