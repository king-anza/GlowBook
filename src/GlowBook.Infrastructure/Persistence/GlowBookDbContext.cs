using GlowBook.Infrastructure.Identity;
using GlowBook.Domain.Professionals;
using GlowBook.Domain.Catalogue;
using GlowBook.Domain.Availability;
using GlowBook.Domain.Bookings;
using GlowBook.Domain.Reviews;
using GlowBook.Domain.Operations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GlowBook.Infrastructure.Persistence;

public sealed class GlowBookDbContext(DbContextOptions<GlowBookDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessLocation> BusinessLocations => Set<BusinessLocation>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<PortfolioImage> PortfolioImages => Set<PortfolioImage>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceOffering> ServiceOfferings => Set<ServiceOffering>();
    public DbSet<WeeklyAvailability> WeeklyAvailabilities => Set<WeeklyAvailability>();
    public DbSet<AvailabilityException> AvailabilityExceptions => Set<AvailabilityException>();
    public DbSet<BlockedTime> BlockedTimes => Set<BlockedTime>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingStatusHistory> BookingStatusHistory => Set<BookingStatusHistory>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        var customerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var professionalRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var adminRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        builder.Entity<IdentityRole<Guid>>().HasData(
            new IdentityRole<Guid> { Id = customerRoleId, Name = "Customer", NormalizedName = "CUSTOMER", ConcurrencyStamp = "customer-role-v1" },
            new IdentityRole<Guid> { Id = professionalRoleId, Name = "Professional", NormalizedName = "PROFESSIONAL", ConcurrencyStamp = "professional-role-v1" },
            new IdentityRole<Guid> { Id = adminRoleId, Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "admin-role-v1" });
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.AccountStatus).HasMaxLength(30).IsRequired();
            entity.Property(x => x.TermsVersion).HasMaxLength(30).IsRequired();
            entity.Property(x => x.PrivacyNoticeVersion).HasMaxLength(30).IsRequired();
        });
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.RevocationReason).HasMaxLength(100);
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
            entity.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<Business>(entity =>
        {
            entity.Property(x => x.TradingName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.CancellationPolicy).HasMaxLength(2000);
            entity.Property(x => x.PublicEmail).HasMaxLength(256);
            entity.Property(x => x.PublicPhone).HasMaxLength(30);
            entity.Property(x => x.ApprovalStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(x => x.OwnerUserId).IsUnique();
            entity.HasOne<ApplicationUser>().WithOne().HasForeignKey<Business>(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<BusinessLocation>(entity =>
        {
            entity.Property(x => x.AddressLine1).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Province).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.BusinessId).HasFilter("\"IsPrimary\" = true").IsUnique();
            entity.HasOne(x => x.Business).WithMany(x => x.Locations).HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<StaffMember>(entity =>
        {
            entity.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Biography).HasMaxLength(1000);
            entity.HasOne(x => x.Business).WithMany(x => x.Staff).HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<PortfolioImage>(entity =>
        {
            entity.Property(x => x.ImageUrl).HasMaxLength(2048).IsRequired(); entity.Property(x => x.AltText).HasMaxLength(200).IsRequired(); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(x => new { x.BusinessId, x.DisplayOrder }).IsUnique();
            entity.HasOne(x => x.Business).WithMany(x => x.PortfolioImages).HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<ServiceCategory>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired(); entity.Property(x => x.Slug).HasMaxLength(100).IsRequired(); entity.HasIndex(x => x.Slug).IsUnique();
            var names = new[] { "Hair", "Barbering", "Nails", "Makeup", "Skincare and Facials", "Eyebrows and Lashes", "Hair Removal", "Massage and Body Treatments", "Bridal and Special Occasion", "Other Beauty Services" };
            entity.HasData(names.Select((name, i) => new ServiceCategory { Id = Guid.Parse($"44444444-4444-4444-4444-{i + 1:000000000000}"), Name = name, Slug = name.ToLowerInvariant().Replace(" and ", "-").Replace(" ", "-"), DisplayOrder = i + 1, IsActive = true }));
        });
        builder.Entity<Service>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired(); entity.Property(x => x.Description).HasMaxLength(2000); entity.Property(x => x.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(x => new { x.BusinessId, x.Name }); entity.HasOne(x => x.Business).WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<ServiceOffering>(entity =>
        {
            entity.Property(x => x.Price).HasPrecision(10, 2); entity.Property(x => x.ConcurrencyToken).IsConcurrencyToken();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_ServiceOffering_Price", "\"Price\" >= 0 AND \"Price\" <= 999999.99");
                table.HasCheckConstraint("CK_ServiceOffering_Duration", "\"DurationMinutes\" BETWEEN 5 AND 720 AND \"DurationMinutes\" % 5 = 0");
                table.HasCheckConstraint("CK_ServiceOffering_Buffer", "\"BufferMinutes\" BETWEEN 0 AND 180 AND \"BufferMinutes\" % 5 = 0");
            });
            entity.HasIndex(x => new { x.ServiceId, x.StaffMemberId, x.BusinessLocationId }).IsUnique();
            entity.HasOne(x => x.Service).WithMany(x => x.Offerings).HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.StaffMember).WithMany().HasForeignKey(x => x.StaffMemberId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.BusinessLocation).WithMany().HasForeignKey(x => x.BusinessLocationId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<WeeklyAvailability>(entity =>
        {
            entity.HasIndex(x => new { x.StaffMemberId, x.DayOfWeek, x.StartTime }).IsUnique();
            entity.ToTable(table => table.HasCheckConstraint("CK_WeeklyAvailability_Time", "\"StartTime\" < \"EndTime\""));
            entity.HasOne(x => x.StaffMember).WithMany().HasForeignKey(x => x.StaffMemberId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<AvailabilityException>(entity =>
        {
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Reason).HasMaxLength(300);
            entity.HasIndex(x => new { x.StaffMemberId, x.Date });
            entity.HasOne(x => x.StaffMember).WithMany().HasForeignKey(x => x.StaffMemberId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<BlockedTime>(entity =>
        {
            entity.Property(x => x.Reason).HasMaxLength(300); entity.HasIndex(x => new { x.StaffMemberId, x.StartUtc });
            entity.ToTable(table => table.HasCheckConstraint("CK_BlockedTime_Range", "\"StartUtc\" < \"EndUtc\""));
            entity.HasOne(x => x.StaffMember).WithMany().HasForeignKey(x => x.StaffMemberId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<Booking>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.ServiceNameSnapshot).HasMaxLength(150); entity.Property(x => x.PriceSnapshot).HasPrecision(10, 2); entity.Property(x => x.CustomerNotes).HasMaxLength(500); entity.Property(x => x.IdempotencyKey).HasMaxLength(100); entity.Property(x => x.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(x => new { x.CustomerUserId, x.IdempotencyKey }).IsUnique(); entity.HasIndex(x => new { x.StaffMemberId, x.StartUtc, x.EndUtc });
            entity.ToTable(table => table.HasCheckConstraint("CK_Booking_Range", "\"StartUtc\" < \"EndUtc\""));
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CustomerUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Business).WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.BusinessLocation).WithMany().HasForeignKey(x => x.BusinessLocationId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.StaffMember).WithMany().HasForeignKey(x => x.StaffMemberId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.ServiceOffering).WithMany().HasForeignKey(x => x.ServiceOfferingId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<BookingStatusHistory>(entity =>
        {
            entity.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.Reason).HasMaxLength(500); entity.HasIndex(x => new { x.BookingId, x.ChangedAtUtc }); entity.HasOne(x => x.Booking).WithMany(x => x.StatusHistory).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<Review>(entity =>
        {
            entity.Property(x => x.Comment).HasMaxLength(1000); entity.Property(x => x.ModerationStatus).HasConversion<string>().HasMaxLength(30); entity.Property(x => x.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(x => x.BookingId).IsUnique(); entity.HasIndex(x => new { x.BusinessId, x.ModerationStatus, x.CreatedAtUtc });
            entity.ToTable(table => table.HasCheckConstraint("CK_Review_Rating", "\"Rating\" BETWEEN 1 AND 5"));
            entity.HasOne(x => x.Booking).WithOne().HasForeignKey<Review>(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Business).WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CustomerUserId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Notification>(entity =>
        {
            entity.Property(x => x.EventType).HasMaxLength(100).IsRequired(); entity.Property(x => x.SubjectType).HasMaxLength(50).IsRequired(); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(x => new { x.Status, x.CreatedAtUtc }); entity.HasIndex(x => new { x.RecipientUserId, x.CreatedAtUtc });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<AuditLog>(entity =>
        {
            entity.Property(x => x.Action).HasMaxLength(100).IsRequired(); entity.Property(x => x.SubjectType).HasMaxLength(50).IsRequired(); entity.Property(x => x.Details).HasMaxLength(1000);
            entity.HasIndex(x => new { x.SubjectType, x.SubjectId, x.OccurredAtUtc }); entity.HasIndex(x => x.OccurredAtUtc);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
