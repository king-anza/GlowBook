using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GlowBook.Application.Identity;
using GlowBook.Infrastructure.Identity;
using GlowBook.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GlowBook.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class AuthenticationJourneyTests
{
    private const string Email = "auth@example.test";
    private const string Password = "ValidPass!123";

    [Fact]
    public async Task Refresh_rotation_reuse_detection_and_logout_revoke_tokens()
    {
        if (!HasPostgreSql()) return;
        await using var factory = new PostgreSqlApplicationFactory();
        await ResetAsync(factory.Services);
        using var client = factory.CreateClient();
        await RegisterAndConfirmAsync(client, factory.Services);
        var original = await LoginAsync(client, Password);

        using var secondClient = factory.CreateClient();
        var attempts = await Task.WhenAll(
            client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original.RefreshToken }),
            secondClient.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = original.RefreshToken }));
        var rotatedResponse = Assert.Single(attempts, response => response.IsSuccessStatusCode);
        Assert.Single(attempts, response => response.StatusCode == HttpStatusCode.BadRequest);
        var rotated = await ReadTokensAsync(rotatedResponse);
        Assert.NotEqual(original.RefreshToken, rotated.RefreshToken);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = rotated.RefreshToken })).StatusCode);

        var fresh = await LoginAsync(client, Password);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = fresh.RefreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = fresh.RefreshToken })).StatusCode);
    }

    [Fact]
    public async Task Password_recovery_is_non_enumerating_and_reset_changes_the_password()
    {
        if (!HasPostgreSql()) return;
        await using var factory = new PostgreSqlApplicationFactory();
        await ResetAsync(factory.Services);
        using var client = factory.CreateClient();
        await RegisterAndConfirmAsync(client, factory.Services);
        var beforeReset = await LoginAsync(client, Password);

        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "missing@example.test" })).StatusCode);
        string resetToken;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            resetToken = await users.GeneratePasswordResetTokenAsync((await users.FindByEmailAsync(Email))!);
        }
        const string newPassword = "ChangedPass!456";
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { email = Email, token = resetToken, newPassword })).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", beforeReset.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/me")).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email, password = Password })).StatusCode);
        await LoginAsync(client, newPassword);
    }

    private static bool HasPostgreSql() => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GLOWBOOK_TEST_CONNECTION"));

    private static async Task ResetAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GlowBookDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    private static async Task RegisterAndConfirmAsync(HttpClient client, IServiceProvider services)
    {
        var registration = await client.PostAsJsonAsync("/api/v1/auth/register/customer", new { email = Email, password = Password, displayName = "Auth Test", acceptTerms = true, confirmMinimumAge = true });
        Assert.Equal(HttpStatusCode.Accepted, registration.StatusCode);
        await using var scope = services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(Email);
        Assert.NotNull(user);
        user.EmailConfirmed = true;
        Assert.True((await users.UpdateAsync(user)).Succeeded);
    }

    private static async Task<AuthTokens> LoginAsync(HttpClient client, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email, password });
        return await ReadTokensAsync(response);
    }

    private static async Task<AuthTokens> ReadTokensAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {body}");
        return (await response.Content.ReadFromJsonAsync<AuthTokens>())!;
    }
}
