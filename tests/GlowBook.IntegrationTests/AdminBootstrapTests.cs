using System.Net;
using System.Net.Http.Json;
using GlowBook.Application.Identity;
using GlowBook.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GlowBook.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class AdminBootstrapTests
{
    [Fact]
    public async Task Temporary_configuration_creates_an_audited_initial_administrator()
    {
        var connection = Environment.GetEnvironmentVariable("GLOWBOOK_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection)) return;
        var options = new DbContextOptionsBuilder<GlowBookDbContext>().UseNpgsql(connection).Options;
        await using (var db = new GlowBookDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        }

        await using var factory = new BootstrapFactory(connection);
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "bootstrap-admin@example.test", password = "AdminPass!123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tokens = await login.Content.ReadFromJsonAsync<AuthTokens>();
        Assert.NotNull(tokens);
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/audit-logs?page=1&pageSize=20")).StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var stored = scope.ServiceProvider.GetRequiredService<GlowBookDbContext>();
        Assert.Equal(1, await stored.AuditLogs.CountAsync(x => x.Action == "AdminBootstrapped"));
    }

    private sealed class BootstrapFactory(string connection) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:GlowBook"] = connection,
                    ["Jwt:Issuer"] = "GlowBook.Tests",
                    ["Jwt:Audience"] = "GlowBook.Tests",
                    ["Jwt:SigningKey"] = "test-only-signing-key-at-least-thirty-two-bytes",
                    ["AdminBootstrap:Email"] = "bootstrap-admin@example.test",
                    ["AdminBootstrap:Password"] = "AdminPass!123"
                }));
    }
}
