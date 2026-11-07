using GlowBook.Application.Operations;
using GlowBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GlowBook.Infrastructure.Operations;

internal sealed class OperationsService(GlowBookDbContext db) : IOperationsService
{
    public async Task<IReadOnlyCollection<AdminUserDto>> UsersAsync(int page, int pageSize, CancellationToken ct) => await db.Users.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Skip((Math.Clamp(page, 1, 10_000) - 1) * Math.Clamp(pageSize, 1, 100)).Take(Math.Clamp(pageSize, 1, 100)).Select(x => new AdminUserDto(x.Id, x.Email ?? string.Empty, x.DisplayName, x.AccountStatus, x.CreatedAtUtc)).ToListAsync(ct);
    public async Task<IReadOnlyCollection<AuditLogDto>> AuditAsync(int page, int pageSize, CancellationToken ct) => await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).Skip((Math.Clamp(page, 1, 10_000) - 1) * Math.Clamp(pageSize, 1, 100)).Take(Math.Clamp(pageSize, 1, 100)).Select(x => new AuditLogDto(x.Id, x.ActorUserId, x.Action, x.SubjectType, x.SubjectId, x.Details, x.OccurredAtUtc)).ToListAsync(ct);
}
