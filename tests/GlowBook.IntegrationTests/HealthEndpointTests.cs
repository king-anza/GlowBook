using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace GlowBook.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<HealthEndpointTests.TestApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(TestApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealthReturnsHealthyStatus()
    {
        var response = await _client.GetAsync("/health");
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Healthy", body.Status);
    }

    [Fact]
    public async Task GetCurrentUserWithoutTokenReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfessionalBusinessWithoutTokenReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/professional/business");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/bookings")]
    [InlineData("/api/v1/admin/users")]
    [InlineData("/api/v1/admin/audit-logs")]
    [InlineData("/api/v1/me/data")]
    public async Task Protected_resources_without_token_return_unauthorized(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("traceId", await response.Content.ReadAsStringAsync());
    }

    private sealed record HealthResponse(string Status);

    public sealed class TestApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Jwt:SigningKey", "test-only-signing-key-at-least-thirty-two-bytes");
            builder.UseSetting("ConnectionStrings:GlowBook", "Host=localhost;Database=glowbook_test;Username=test");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:GlowBook"] = "Host=localhost;Database=glowbook_test;Username=test",
                    ["Jwt:Issuer"] = "GlowBook.Tests",
                    ["Jwt:Audience"] = "GlowBook.Tests",
                    ["Jwt:SigningKey"] = "test-only-signing-key-at-least-thirty-two-bytes"
                }));
        }
    }
}
