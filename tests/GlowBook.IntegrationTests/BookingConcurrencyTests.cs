using GlowBook.Application.Bookings;
using GlowBook.Domain.Availability;
using GlowBook.Domain.Catalogue;
using GlowBook.Domain.Professionals;
using GlowBook.Infrastructure.Identity;
using GlowBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GlowBook.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class BookingConcurrencyTests
{
    [Fact]
    public async Task Concurrent_requests_for_same_staff_slot_create_one_booking()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GLOWBOOK_TEST_CONNECTION"))) return;
        await using var factory = new PostgreSqlApplicationFactory();
        var seed = await SeedAsync(factory.Services);
        var start = seed.StartUtc;

        async Task<BookingDto?> Book(Guid customerId, string key)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IBookingService>();
            return await service.CreateAsync(customerId, new CreateBookingRequest(seed.OfferingId, new DateTimeOffset(start, TimeSpan.Zero), key, null), CancellationToken.None);
        }

        var results = await Task.WhenAll(Book(seed.CustomerOneId, "request-one"), Book(seed.CustomerTwoId, "request-two"));
        var booking = Assert.Single(results, x => x is not null)!;
        Assert.Equal(start.AddMinutes(45), booking.EndUtc);

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<GlowBookDbContext>();
        Assert.Equal(1, await db.Bookings.CountAsync());
    }

    private static async Task<SeedResult> SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GlowBookDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        var owner = User("owner@example.test"); var one = User("one@example.test"); var two = User("two@example.test");
        db.Users.AddRange(owner, one, two);
        var business = new Business { Id = Guid.NewGuid(), OwnerUserId = owner.Id, TradingName = "Concurrency Salon", ApprovalStatus = BusinessApprovalStatus.Approved, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var location = new BusinessLocation { Id = Guid.NewGuid(), Business = business, IsPrimary = true, AddressLine1 = "1 Test Street", Suburb = "Sandton", City = "Johannesburg", Province = "Gauteng", PostalCode = "2196", TimeZone = "Africa/Johannesburg" };
        var staff = new StaffMember { Id = Guid.NewGuid(), Business = business, DisplayName = "Test Stylist", IsActive = true, AcceptingBookings = true, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var categoryId = Guid.Parse("44444444-4444-4444-4444-000000000001");
        var service = new Service { Id = Guid.NewGuid(), Business = business, CategoryId = categoryId, Name = "Test Service", IsActive = true, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var offering = new ServiceOffering { Id = Guid.NewGuid(), Service = service, StaffMember = staff, BusinessLocation = location, Price = 100, DurationMinutes = 30, BufferMinutes = 15, IsActive = true, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };

        var localDate = NextWeekday(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), DayOfWeek.Monday);
        var localStart = localDate.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg"));
        db.AddRange(business, location, staff, service, offering, new WeeklyAvailability { Id = Guid.NewGuid(), StaffMember = staff, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0) });
        await db.SaveChangesAsync();
        return new(offering.Id, one.Id, two.Id, startUtc);
    }

    private static ApplicationUser User(string email) => new() { Id = Guid.NewGuid(), Email = email, NormalizedEmail = email.ToUpperInvariant(), UserName = email, NormalizedUserName = email.ToUpperInvariant(), DisplayName = email, AccountStatus = "Active", EmailConfirmed = true, TermsVersion = "test", TermsAcceptedAtUtc = DateTime.UtcNow, PrivacyNoticeVersion = "test", PrivacyNoticeAcceptedAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow, SecurityStamp = Guid.NewGuid().ToString() };
    private static DateOnly NextWeekday(DateOnly date, DayOfWeek day) { while (date.DayOfWeek != day) date = date.AddDays(1); return date; }
    private sealed record SeedResult(Guid OfferingId, Guid CustomerOneId, Guid CustomerTwoId, DateTime StartUtc);
}
