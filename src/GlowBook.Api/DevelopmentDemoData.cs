using GlowBook.Domain.Availability;
using GlowBook.Domain.Bookings;
using GlowBook.Domain.Catalogue;
using GlowBook.Domain.Professionals;
using GlowBook.Domain.Reviews;
using GlowBook.Infrastructure.Identity;
using GlowBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GlowBook.Api;

internal static class DevelopmentDemoData
{
    private sealed record Studio(string Id, string Name, string Area, string Category, string Service, decimal Price, int Minutes, string Image, string Description);

    private static readonly Studio[] Studios =
    [
        new("10000000-0000-0000-0000-000000000001", "Nomsa Braid Studio", "Walmer", "Hair", "Small knotless braids", 320, 180, "braids.webp", "Neat, lightweight braids with gentle tension and clean parting."),
        new("10000000-0000-0000-0000-000000000002", "Lumi Nail Bar", "Newton Park", "Nails", "Gel overlay manicure", 280, 60, "nails.webp", "Detailed nail preparation and soft, clean finishes."),
        new("10000000-0000-0000-0000-000000000003", "Zola Artistry", "Summerstrand", "Makeup", "Soft glam makeup", 520, 60, "makeup.webp", "Skin-first soft glam for events, celebrations and photos."),
        new("10000000-0000-0000-0000-000000000004", "Nandi Lash Studio", "Central", "Eyebrows and Lashes", "Hybrid lash extensions", 550, 110, "lashes.webp", "Natural-looking lash sets, soft volume and tidy brow shaping."),
        new("10000000-0000-0000-0000-000000000005", "Imara Natural Hair", "Mount Croix", "Hair", "Two-strand twists", 350, 105, "beauty.webp", "Textured-hair styling, gentle detangling and protective styles."),
        new("10000000-0000-0000-0000-000000000006", "Abeni Beauty Room", "Richmond Hill", "Eyebrows and Lashes", "Brow shape and tint", 260, 45, "lashes.webp", "Careful brow shaping and softly defined, natural-looking colour."),
    ];

    public static async Task<int> SeedAsync(GlowBookDbContext db, CancellationToken ct)
    {
        if (await db.Businesses.AnyAsync(x => x.TradingName == "Nomsa Braid Studio", ct)) return 0;
        var now = DateTime.UtcNow;
        var customer = new ApplicationUser { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), UserName = "demo.customer@glowbook.local", NormalizedUserName = "DEMO.CUSTOMER@GLOWBOOK.LOCAL", Email = "demo.customer@glowbook.local", NormalizedEmail = "DEMO.CUSTOMER@GLOWBOOK.LOCAL", EmailConfirmed = true, DisplayName = "Gqeberha Glow Guest", TermsAcceptedAtUtc = now, PrivacyNoticeAcceptedAtUtc = now, CreatedAtUtc = now, SecurityStamp = "demo-customer" };
        db.Users.Add(customer);
        foreach (var (studio, index) in Studios.Select((value, index) => (value, index)))
        {
            var businessId = Guid.Parse(studio.Id);
            var ownerId = Guid.Parse($"21000000-0000-0000-0000-{index + 1:000000000000}");
            var locationId = Guid.Parse($"22000000-0000-0000-0000-{index + 1:000000000000}");
            var staffId = Guid.Parse($"23000000-0000-0000-0000-{index + 1:000000000000}");
            var serviceId = Guid.Parse($"24000000-0000-0000-0000-{index + 1:000000000000}");
            var offeringId = Guid.Parse($"25000000-0000-0000-0000-{index + 1:000000000000}");
            var categoryId = studio.Category == "Hair" ? Guid.Parse("44444444-4444-4444-4444-000000000001") : studio.Category == "Nails" ? Guid.Parse("44444444-4444-4444-4444-000000000003") : studio.Category == "Makeup" ? Guid.Parse("44444444-4444-4444-4444-000000000004") : Guid.Parse("44444444-4444-4444-4444-000000000006");
            db.Users.Add(new ApplicationUser { Id = ownerId, UserName = $"demo.{index}@glowbook.local", NormalizedUserName = $"DEMO.{index}@GLOWBOOK.LOCAL", Email = $"demo.{index}@glowbook.local", NormalizedEmail = $"DEMO.{index}@GLOWBOOK.LOCAL", EmailConfirmed = true, DisplayName = studio.Name, TermsAcceptedAtUtc = now, PrivacyNoticeAcceptedAtUtc = now, CreatedAtUtc = now, SecurityStamp = $"demo-{index}" });
            db.Businesses.Add(new Business { Id = businessId, OwnerUserId = ownerId, TradingName = studio.Name, Description = studio.Description, CancellationPolicy = "Please cancel at least 24 hours before your appointment.", ApprovalStatus = BusinessApprovalStatus.Approved, ApprovedAtUtc = now, CreatedAtUtc = now, UpdatedAtUtc = now });
            db.BusinessLocations.Add(new BusinessLocation { Id = locationId, BusinessId = businessId, Name = studio.Name, AddressLine1 = $"{12 + index} Main Road", Suburb = studio.Area, City = "Gqeberha", Province = "Eastern Cape", PostalCode = "6001" });
            db.StaffMembers.Add(new StaffMember { Id = staffId, BusinessId = businessId, DisplayName = studio.Name.Split(' ')[0], Biography = studio.Description, CreatedAtUtc = now, UpdatedAtUtc = now });
            db.Services.Add(new Service { Id = serviceId, BusinessId = businessId, CategoryId = categoryId, Name = studio.Service, Description = studio.Description, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now });
            db.ServiceOfferings.Add(new ServiceOffering { Id = offeringId, ServiceId = serviceId, StaffMemberId = staffId, BusinessLocationId = locationId, Price = studio.Price, DurationMinutes = studio.Minutes, BufferMinutes = 15, CreatedAtUtc = now, UpdatedAtUtc = now });
            foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday }) db.WeeklyAvailabilities.Add(new WeeklyAvailability { Id = Guid.NewGuid(), StaffMemberId = staffId, DayOfWeek = day, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0) });
            var booking = new Booking { Id = Guid.NewGuid(), CustomerUserId = customer.Id, BusinessId = businessId, BusinessLocationId = locationId, StaffMemberId = staffId, ServiceOfferingId = offeringId, StartUtc = now.AddDays(-14 - index), EndUtc = now.AddDays(-14 - index).AddMinutes(studio.Minutes), Status = BookingStatus.Completed, ServiceNameSnapshot = studio.Service, PriceSnapshot = studio.Price, DurationMinutesSnapshot = studio.Minutes, IdempotencyKey = $"demo-completed-{index}", CreatedAtUtc = now.AddDays(-21), CompletedAtUtc = now.AddDays(-14 - index) };
            db.Bookings.Add(booking);
            db.Reviews.Add(new Review { Id = Guid.NewGuid(), BookingId = booking.Id, CustomerUserId = customer.Id, BusinessId = businessId, Rating = index % 2 == 0 ? 5 : 4, Comment = "Beautiful result, welcoming service and an easy booking experience.", CreatedAtUtc = now.AddDays(-10), UpdatedAtUtc = now.AddDays(-10) });
            db.PortfolioImages.Add(new PortfolioImage { Id = Guid.NewGuid(), BusinessId = businessId, ImageUrl = $"assets/glowbook/{studio.Image}", AltText = $"Portfolio image for {studio.Name}", DisplayOrder = 1, CreatedAtUtc = now });
        }
        await db.SaveChangesAsync(ct);
        return Studios.Length;
    }
}
