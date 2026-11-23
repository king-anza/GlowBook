using GlowBook.Domain.Operations;
using GlowBook.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GlowBook.Infrastructure.Identity;

internal sealed class AdminBootstrapService(
    IServiceScopeFactory scopeFactory,
    IOptions<AdminBootstrapOptions> options,
    ILogger<AdminBootstrapService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var configuration = options.Value;
        var configured = !string.IsNullOrWhiteSpace(configuration.Email) || !string.IsNullOrWhiteSpace(configuration.Password);
        if (!configured) return;
        if (string.IsNullOrWhiteSpace(configuration.Email) || string.IsNullOrWhiteSpace(configuration.Password))
            throw new InvalidOperationException("Admin bootstrap requires both an email and password.");

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GlowBookDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext('glowbook-admin-bootstrap'))", cancellationToken);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if ((await users.GetUsersInRoleAsync("Admin")).Count != 0)
        {
            logger.LogInformation("Administrator bootstrap was skipped because an administrator already exists.");
            return;
        }

        var email = configuration.Email.Trim();
        var user = await users.FindByEmailAsync(email);
        if (user is not null)
            throw new InvalidOperationException("The administrator bootstrap email already belongs to a non-administrator account.");

        var now = DateTime.UtcNow;
        user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "GlowBook Administrator",
            AccountStatus = "Active",
            TermsVersion = "operator-bootstrap",
            TermsAcceptedAtUtc = now,
            PrivacyNoticeVersion = "operator-bootstrap",
            PrivacyNoticeAcceptedAtUtc = now,
            CreatedAtUtc = now
        };
        EnsureSucceeded(await users.CreateAsync(user, configuration.Password), "Administrator account creation failed.");

        EnsureSucceeded(await users.AddToRoleAsync(user, "Admin"), "Administrator role assignment failed.");
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = "AdminBootstrapped",
            SubjectType = "User",
            SubjectId = user.Id,
            OccurredAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("The initial administrator was bootstrapped. Remove the bootstrap configuration now.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded) throw new InvalidOperationException($"{message} Identity error codes: {string.Join(", ", result.Errors.Select(x => x.Code))}");
    }
}
