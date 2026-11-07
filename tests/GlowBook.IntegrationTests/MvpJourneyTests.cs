using System.Net.Http.Headers;
using System.Net.Http.Json;
using GlowBook.Application.Bookings;
using GlowBook.Application.Accounts;
using GlowBook.Application.Catalogue;
using GlowBook.Application.Identity;
using GlowBook.Application.Professionals;
using GlowBook.Application.Reviews;
using GlowBook.Domain.Bookings;
using GlowBook.Infrastructure.Identity;
using GlowBook.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GlowBook.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class MvpJourneyTests
{
    [Fact]
    public async Task Registration_to_completed_booking_and_review_works_against_PostgreSQL()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GLOWBOOK_TEST_CONNECTION"))) return;
        await using var factory = new PostgreSqlApplicationFactory();
        await ResetDatabaseAsync(factory.Services);
        using var client = factory.CreateClient();

        const string password = "ValidPass!123";
        await RegisterAsync(client, "professional@example.test", password, "Professional", true);
        await RegisterAsync(client, "customer@example.test", password, "Customer", false);
        await RegisterAsync(client, "intruder@example.test", password, "Intruder", false);
        var duplicate = await client.PostAsJsonAsync("/api/v1/auth/register/customer", new { email = "customer@example.test", password, displayName = "Duplicate", acceptTerms = true, confirmMinimumAge = true });
        var duplicateBody = await duplicate.Content.ReadAsStringAsync();
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.DoesNotContain("already", duplicateBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exists", duplicateBody, StringComparison.OrdinalIgnoreCase);
        await ConfirmEmailsAndCreateAdminAsync(factory.Services, password);

        var professionalToken = await LoginAsync(client, "professional@example.test", password);
        var customerToken = await LoginAsync(client, "customer@example.test", password);
        var adminToken = await LoginAsync(client, "admin@example.test", password);
        var intruderToken = await LoginAsync(client, "intruder@example.test", password);

        Authorize(client, professionalToken);
        var business = await PostAsync<BusinessDto>(client, "/api/v1/professional/business", new
        {
            tradingName = "Glow API Studio", description = "Integration test studio", cancellationPolicy = "Cancel anytime", publicEmail = "studio@example.test", publicPhone = (string?)null,
            location = new { addressLine1 = "1 Main Road", addressLine2 = (string?)null, suburb = "Sandton", city = "Johannesburg", province = "Gauteng", postalCode = "2196" }, concurrencyToken = (Guid?)null
        });
        var staffId = Assert.Single(business.Staff).Id;
        var portfolioImage = await PostAsync<PortfolioImageDto>(client, "/api/v1/professional/portfolio-images", new { imageUrl = "https://example.test/portfolio.jpg", altText = "Finished silk press hairstyle", displayOrder = 0 });
        var categoryId = Guid.Parse("44444444-4444-4444-4444-000000000001");
        var service = await PostAsync<ServiceDto>(client, "/api/v1/professional/services", new { categoryId, name = "Silk press", description = "Wash and style", concurrencyToken = (Guid?)null });
        var offering = await PostAsync<OfferingDto>(client, $"/api/v1/professional/services/{service.Id}/offerings", new { staffMemberId = staffId, price = 250m, durationMinutes = 30, bufferMinutes = 0, concurrencyToken = (Guid?)null });
        await SuccessAsync(client.PostAsJsonAsync($"/api/v1/professional/services/{service.Id}/activate", new { }));

        var localDate = NextWeekday(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), DayOfWeek.Monday);
        await SuccessAsync(client.PutAsJsonAsync($"/api/v1/professional/staff/{staffId}/weekly-availability", new[] { new { dayOfWeek = (int)DayOfWeek.Monday, startTime = "09:00:00", endTime = "11:00:00" } }));
        await SuccessAsync(client.PostAsJsonAsync("/api/v1/professional/business/submit", new { }));

        Authorize(client, adminToken);
        await SuccessAsync(client.PostAsJsonAsync($"/api/v1/admin/businesses/{business.Id}/approve", new { }));

        client.DefaultRequestHeaders.Authorization = null;
        var discovered = await client.GetAsync($"/api/v1/businesses?search=Glow%20API&categoryId={categoryId}&province=Gauteng&page=1&pageSize=20");
        await SuccessAsync(Task.FromResult(discovered));
        var outsidePriceRange = await client.GetFromJsonAsync<PagedBusinesses>($"/api/v1/businesses?minPrice=300&maxPrice=500&page=1&pageSize=20");
        Assert.NotNull(outsidePriceRange);
        Assert.Equal(0, outsidePriceRange.TotalCount);
        var slots = await client.GetFromJsonAsync<List<Slot>>($"/api/v1/staff/{staffId}/availability?offeringId={offering.Id}&from={localDate:yyyy-MM-dd}&to={localDate:yyyy-MM-dd}");
        Assert.NotNull(slots); Assert.True(slots.Count >= 2);

        Authorize(client, customerToken);
        var first = await PostAsync<BookingDto>(client, "/api/v1/bookings", new { offeringId = offering.Id, startUtc = slots[0].StartUtc, idempotencyKey = "journey-complete", notes = "Please avoid scented products." });
        Authorize(client, professionalToken);
        var initialProfessionalBookings = await client.GetFromJsonAsync<List<BookingDto>>("/api/v1/professional/bookings");
        var pending = Assert.Single(initialProfessionalBookings!, x => x.Id == first.Id);
        Assert.Equal(first.ConcurrencyToken, pending.ConcurrencyToken);
        await SuccessAsync(client.PostAsJsonAsync($"/api/v1/professional/bookings/{first.Id}/confirm", new { reason = (string?)null, concurrencyToken = pending.ConcurrencyToken }));
        var professionalBookings = await client.GetFromJsonAsync<List<BookingDto>>("/api/v1/professional/bookings");
        var confirmed = Assert.Single(professionalBookings!, x => x.Id == first.Id);
        await SuccessAsync(client.PostAsJsonAsync($"/api/v1/professional/bookings/{first.Id}/complete", new { reason = (string?)null, concurrencyToken = confirmed.ConcurrencyToken }));

        Authorize(client, customerToken);
        var review = await PostAsync<ReviewDto>(client, $"/api/v1/bookings/{first.Id}/review", new { rating = 5, comment = "Excellent service", concurrencyToken = (Guid?)null });
        Assert.Equal(5, review.Rating);

        Authorize(client, adminToken);
        var moderationReviews = await client.GetFromJsonAsync<List<AdminReviewDto>>("/api/v1/admin/reviews");
        Assert.Contains(moderationReviews!, x => x.Id == review.Id && x.Status == "Active");
        await SuccessAsync(client.PostAsJsonAsync($"/api/v1/admin/reviews/{review.Id}/hide", new { }));
        await SuccessAsync(client.PostAsJsonAsync($"/api/v1/admin/reviews/{review.Id}/restore", new { }));
        var moderationImages = await client.GetFromJsonAsync<List<AdminPortfolioImageDto>>("/api/v1/admin/portfolio-images");
        Assert.Contains(moderationImages!, x => x.Id == portfolioImage.Id && x.Status == GlowBook.Domain.Professionals.PortfolioImageStatus.Active);
        await SuccessAsync(client.PostAsJsonAsync($"/api/v1/admin/portfolio-images/{portfolioImage.Id}/hide", new { }));
        await SuccessAsync(client.PostAsJsonAsync($"/api/v1/admin/portfolio-images/{portfolioImage.Id}/restore", new { }));

        Authorize(client, customerToken);
        var second = await PostAsync<BookingDto>(client, "/api/v1/bookings", new { offeringId = offering.Id, startUtc = slots[1].StartUtc, idempotencyKey = "journey-cancel", notes = (string?)null });
        Authorize(client, intruderToken);
        var forbiddenCustomerAction = await client.PostAsJsonAsync($"/api/v1/bookings/{second.Id}/cancel", new { reason = "Not mine", concurrencyToken = second.ConcurrencyToken });
        Assert.Equal(System.Net.HttpStatusCode.Conflict, forbiddenCustomerAction.StatusCode);
        var forbiddenProfessionalAction = await client.PostAsJsonAsync($"/api/v1/professional/bookings/{second.Id}/confirm", new { reason = "Not my business", concurrencyToken = second.ConcurrencyToken });
        Assert.Equal(System.Net.HttpStatusCode.Conflict, forbiddenProfessionalAction.StatusCode);
        Authorize(client, customerToken);
        await SuccessAsync(client.PostAsJsonAsync($"/api/v1/bookings/{second.Id}/cancel", new { reason = "Plans changed", concurrencyToken = second.ConcurrencyToken }));
        var customerBookings = await client.GetFromJsonAsync<List<BookingDto>>("/api/v1/bookings");
        Assert.Contains(customerBookings!, x => x.Id == second.Id && x.Status == BookingStatus.Cancelled);

        var accountData = await client.GetFromJsonAsync<AccountDataDto>("/api/v1/me/data");
        Assert.NotNull(accountData);
        Assert.Equal(2, accountData.Bookings.Count);
        var correction = await client.PatchAsJsonAsync("/api/v1/me", new { email = "customer@example.test", displayName = "Corrected Customer", marketingConsent = true });
        await SuccessAsync(Task.FromResult(correction));
        Assert.Equal(System.Net.HttpStatusCode.NoContent, (await client.DeleteAsync("/api/v1/me")).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/bookings")).StatusCode);
    }

    private static async Task ResetDatabaseAsync(IServiceProvider services) { await using var scope = services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<GlowBookDbContext>(); await db.Database.EnsureDeletedAsync(); await db.Database.MigrateAsync(); }
    private static async Task RegisterAsync(HttpClient client, string email, string password, string name, bool professional) { var response = await client.PostAsJsonAsync($"/api/v1/auth/register/{(professional ? "professional" : "customer")}", new { email, password, displayName = name, acceptTerms = true, confirmMinimumAge = true }); await SuccessAsync(Task.FromResult(response)); }
    private static async Task ConfirmEmailsAndCreateAdminAsync(IServiceProvider services, string password) { await using var scope = services.CreateAsyncScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); foreach (var email in new[] { "professional@example.test", "customer@example.test", "intruder@example.test" }) { var user = await users.FindByEmailAsync(email); Assert.NotNull(user); user.EmailConfirmed = true; Assert.True((await users.UpdateAsync(user)).Succeeded); if (email == "intruder@example.test") Assert.True((await users.AddToRoleAsync(user, "Professional")).Succeeded); } var admin = new ApplicationUser { Id = Guid.NewGuid(), UserName = "admin@example.test", Email = "admin@example.test", EmailConfirmed = true, DisplayName = "Admin", AccountStatus = "Active", TermsVersion = "test", TermsAcceptedAtUtc = DateTime.UtcNow, PrivacyNoticeVersion = "test", PrivacyNoticeAcceptedAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow }; Assert.True((await users.CreateAsync(admin, password)).Succeeded); Assert.True((await users.AddToRoleAsync(admin, "Admin")).Succeeded); }
    private static async Task<string> LoginAsync(HttpClient client, string email, string password) { var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password }); await SuccessAsync(Task.FromResult(response)); return (await response.Content.ReadFromJsonAsync<AuthTokens>())!.AccessToken; }
    private static void Authorize(HttpClient client, string token) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    private static async Task<T> PostAsync<T>(HttpClient client, string path, object body) { var response = await client.PostAsJsonAsync(path, body); await SuccessAsync(Task.FromResult(response)); return (await response.Content.ReadFromJsonAsync<T>())!; }
    private static async Task SuccessAsync(Task<HttpResponseMessage> responseTask) { var response = await responseTask; var body = await response.Content.ReadAsStringAsync(); Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode} {response.ReasonPhrase}: {body}"); }
    private static DateOnly NextWeekday(DateOnly date, DayOfWeek day) { while (date.DayOfWeek != day) date = date.AddDays(1); return date; }
    private sealed record Slot(DateTime StartUtc, DateTime EndUtc);
    private sealed record PagedBusinesses(int TotalCount);
}
