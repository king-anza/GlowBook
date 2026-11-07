using GlowBook.Application.Professionals;
using GlowBook.Domain.Professionals;
using GlowBook.Domain.Operations;
using GlowBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GlowBook.Infrastructure.Professionals;

internal sealed class ProfessionalService(GlowBookDbContext db) : IProfessionalService
{
    public async Task<BusinessDto?> GetAsync(Guid ownerId, CancellationToken ct) => Map(await Query().SingleOrDefaultAsync(x => x.OwnerUserId == ownerId, ct));

    public async Task<BusinessDto> SaveAsync(Guid ownerId, string ownerName, BusinessRequest request, CancellationToken ct)
    {
        var business = await Query().SingleOrDefaultAsync(x => x.OwnerUserId == ownerId, ct);
        if (business is null)
        {
            business = new Business { Id = Guid.NewGuid(), OwnerUserId = ownerId, CreatedAtUtc = DateTime.UtcNow };
            business.Locations.Add(new BusinessLocation { Id = Guid.NewGuid() });
            business.Staff.Add(new StaffMember { Id = Guid.NewGuid(), LinkedUserId = ownerId, DisplayName = ownerName, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
            db.Businesses.Add(business);
        }
        else if (request.ConcurrencyToken != business.ConcurrencyToken) throw new DbUpdateConcurrencyException("The profile was changed by another request.");
        business.TradingName = request.TradingName.Trim(); business.Description = request.Description?.Trim() ?? string.Empty; business.CancellationPolicy = request.CancellationPolicy?.Trim() ?? string.Empty; business.PublicEmail = request.PublicEmail?.Trim(); business.PublicPhone = request.PublicPhone?.Trim(); business.UpdatedAtUtc = DateTime.UtcNow; business.ConcurrencyToken = Guid.NewGuid();
        var location = business.Locations.Single(x => x.IsPrimary); location.AddressLine1 = request.Location.AddressLine1.Trim(); location.AddressLine2 = request.Location.AddressLine2?.Trim(); location.Suburb = request.Location.Suburb.Trim(); location.City = request.Location.City.Trim(); location.Province = request.Location.Province.Trim(); location.PostalCode = request.Location.PostalCode.Trim();
        if (business.ApprovalStatus == BusinessApprovalStatus.Approved) business.ApprovalStatus = BusinessApprovalStatus.PendingReview;
        await db.SaveChangesAsync(ct); return Map(business)!;
    }

    public async Task<bool> SubmitAsync(Guid ownerId, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(x => x.Id == ownerId && x.EmailConfirmed && x.AccountStatus == "Active", ct)) return false;
        var business = await Query().SingleOrDefaultAsync(x => x.OwnerUserId == ownerId, ct);
        if (business is null || string.IsNullOrWhiteSpace(business.TradingName) || business.Locations.Count == 0) return false;
        business.ApprovalStatus = BusinessApprovalStatus.PendingReview; business.SubmittedAtUtc = DateTime.UtcNow; business.ReviewReason = null;
        db.AuditLogs.Add(Audit(ownerId, "BusinessSubmitted", business.Id, null)); await db.SaveChangesAsync(ct); return true;
    }

    public async Task<StaffDto?> AddStaffAsync(Guid ownerId, StaffRequest request, CancellationToken ct)
    {
        var businessId = await db.Businesses.Where(x => x.OwnerUserId == ownerId).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (businessId is null) return null;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({businessId.Value.ToString()}))", ct);
        var business = await db.Businesses.Include(x => x.Staff).SingleAsync(x => x.Id == businessId.Value, ct);
        if (business is null || business.Staff.Count >= 25) return null;
        var staff = new StaffMember { Id = Guid.NewGuid(), BusinessId = business.Id, DisplayName = request.DisplayName.Trim(), Biography = request.Biography?.Trim() ?? string.Empty, AcceptingBookings = request.AcceptingBookings, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        db.StaffMembers.Add(staff); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return new(staff.Id, staff.DisplayName, staff.Biography, staff.IsActive, staff.AcceptingBookings);
    }

    public async Task<IReadOnlyCollection<BusinessDto>> PendingAsync(CancellationToken ct) => (await Query().Where(x => x.ApprovalStatus == BusinessApprovalStatus.PendingReview).OrderBy(x => x.SubmittedAtUtc).Take(100).ToListAsync(ct)).Select(x => Map(x)!).ToArray();
    public async Task<bool> ReviewAsync(Guid actorId, Guid id, BusinessApprovalStatus status, string? reason, CancellationToken ct) { var b = await db.Businesses.FindAsync([id], ct); if (b is null) return false; b.ApprovalStatus = status; b.ReviewReason = reason; b.ApprovedAtUtc = status == BusinessApprovalStatus.Approved ? DateTime.UtcNow : null; b.UpdatedAtUtc = DateTime.UtcNow; db.AuditLogs.Add(Audit(actorId, $"Business{status}", id, reason)); await db.SaveChangesAsync(ct); return true; }
    public async Task<PortfolioImageDto?> AddPortfolioImageAsync(Guid ownerId, PortfolioImageRequest request, CancellationToken ct) { var businessId = await db.Businesses.Where(x => x.OwnerUserId == ownerId).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct); if (businessId is null) return null; await using var transaction = await db.Database.BeginTransactionAsync(ct); await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({businessId.Value.ToString()}))", ct); if (await db.PortfolioImages.CountAsync(x => x.BusinessId == businessId, ct) >= 10 || await db.PortfolioImages.AnyAsync(x => x.BusinessId == businessId && x.DisplayOrder == request.DisplayOrder, ct)) return null; var image = new PortfolioImage { Id = Guid.NewGuid(), BusinessId = businessId.Value, ImageUrl = request.ImageUrl.Trim(), AltText = request.AltText.Trim(), DisplayOrder = request.DisplayOrder, CreatedAtUtc = DateTime.UtcNow }; db.PortfolioImages.Add(image); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return new(image.Id, image.ImageUrl, image.AltText, image.DisplayOrder, image.Status); }
    public async Task<IReadOnlyCollection<PortfolioImageDto>> PortfolioImagesAsync(Guid ownerId, CancellationToken ct) => await db.PortfolioImages.AsNoTracking().Where(x => x.Business.OwnerUserId == ownerId).OrderBy(x => x.DisplayOrder).Select(x => new PortfolioImageDto(x.Id, x.ImageUrl, x.AltText, x.DisplayOrder, x.Status)).ToListAsync(ct);
    public async Task<bool> DeletePortfolioImageAsync(Guid ownerId, Guid imageId, CancellationToken ct) { var image = await db.PortfolioImages.SingleOrDefaultAsync(x => x.Id == imageId && x.Business.OwnerUserId == ownerId, ct); if (image is null) return false; db.PortfolioImages.Remove(image); await db.SaveChangesAsync(ct); return true; }
    public async Task<bool> ModeratePortfolioImageAsync(Guid actorId, Guid imageId, bool hidden, CancellationToken ct) { var image = await db.PortfolioImages.SingleOrDefaultAsync(x => x.Id == imageId, ct); if (image is null) return false; image.Status = hidden ? PortfolioImageStatus.Hidden : PortfolioImageStatus.Active; db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ActorUserId = actorId, Action = hidden ? "PortfolioImageHidden" : "PortfolioImageRestored", SubjectType = "PortfolioImage", SubjectId = imageId, OccurredAtUtc = DateTime.UtcNow }); await db.SaveChangesAsync(ct); return true; }
    private IQueryable<Business> Query() => db.Businesses.Include(x => x.Locations).Include(x => x.Staff);
    private static BusinessDto? Map(Business? b) { if (b is null) return null; var l = b.Locations.Single(x => x.IsPrimary); return new(b.Id, b.TradingName, b.Description, b.CancellationPolicy, b.PublicEmail, b.PublicPhone, b.ApprovalStatus, b.ReviewReason, b.ConcurrencyToken, new(l.AddressLine1, l.AddressLine2, l.Suburb, l.City, l.Province, l.PostalCode), b.Staff.Select(s => new StaffDto(s.Id, s.DisplayName, s.Biography, s.IsActive, s.AcceptingBookings)).ToArray()); }
    private static AuditLog Audit(Guid actorId, string action, Guid subjectId, string? details) => new() { Id = Guid.NewGuid(), ActorUserId = actorId, Action = action, SubjectType = "Business", SubjectId = subjectId, Details = details, OccurredAtUtc = DateTime.UtcNow };
}
