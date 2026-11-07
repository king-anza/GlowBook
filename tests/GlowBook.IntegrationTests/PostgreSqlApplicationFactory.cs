using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GlowBook.IntegrationTests;

[CollectionDefinition("PostgreSQL", DisableParallelization = true)]
public sealed class PostgreSqlCollection;

public sealed class PostgreSqlApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connection = Environment.GetEnvironmentVariable("GLOWBOOK_TEST_CONNECTION") ?? "Host=localhost;Port=5432;Database=glowbook_test;Username=postgres;Password=postgres";
        builder.UseSetting("ConnectionStrings:GlowBook", connection);
        builder.UseSetting("Jwt:Issuer", "GlowBook.Tests");
        builder.UseSetting("Jwt:Audience", "GlowBook.Tests");
        builder.UseSetting("Jwt:SigningKey", "test-only-signing-key-at-least-thirty-two-bytes");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:GlowBook"] = connection, ["Jwt:Issuer"] = "GlowBook.Tests", ["Jwt:Audience"] = "GlowBook.Tests", ["Jwt:SigningKey"] = "test-only-signing-key-at-least-thirty-two-bytes" }));
        builder.ConfigureLogging(logging =>
        {
            logging.AddFilter("GlowBook.Infrastructure.Bookings.BookingService", LogLevel.Information);
            logging.AddConsole();
        });
    }
}
