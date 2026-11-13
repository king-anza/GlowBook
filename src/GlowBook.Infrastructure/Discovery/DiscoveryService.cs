using GlowBook.Application.Discovery;
using GlowBook.Domain.Professionals;
using GlowBook.Domain.Reviews;
using GlowBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GlowBook.Infrastructure.Discovery;

internal sealed class DiscoveryService(GlowBookDbContext db) : IDiscoveryService
{
    public async Task<PagedResult<BusinessSummaryDto>> SearchAsync(DiscoveryQuery request, CancellationToken ct)
    {
        var page = Math.Clamp(request.Page, 1, 10_000); var size = Math.Clamp(request.PageSize, 1, 50);
        var query = db.Businesses.AsNoTracking().Where(x => x.ApprovalStatus == BusinessApprovalStatus.Approved)
            .Where(x => x.Locations.Any(l => l.IsPrimary && l.IsActive))
            .Where(x => db.Services.Any(s => s.BusinessId == x.Id && s.IsActive && s.Offerings.Any(o => o.IsActive && o.StaffMember.IsActive && o.StaffMember.AcceptingBookings && o.BusinessLocation.IsActive)));
        if (!string.IsNullOrWhiteSpace(request.Search)) { var term = $"%{request.Search.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.TradingName, term) || EF.Functions.ILike(x.Description, term)); }
        if (request.CategoryId.HasValue) query = query.Where(x => db.Services.Any(s => s.BusinessId == x.Id && s.IsActive && s.CategoryId == request.CategoryId && s.Offerings.Any(o => o.IsActive && o.StaffMember.IsActive && o.StaffMember.AcceptingBookings && o.BusinessLocation.IsActive)));
        if (!string.IsNullOrWhiteSpace(request.Province)) query = query.Where(x => x.Locations.Any(l => l.IsPrimary && l.Province == request.Province));
        if (!string.IsNullOrWhiteSpace(request.City)) query = query.Where(x => x.Locations.Any(l => l.IsPrimary && EF.Functions.ILike(l.City, request.City)));
        if (!string.IsNullOrWhiteSpace(request.Suburb)) query = query.Where(x => x.Locations.Any(l => l.IsPrimary && EF.Functions.ILike(l.Suburb, request.Suburb)));
        if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
            query = query.Where(x => db.ServiceOfferings.Any(o =>
                o.Service.BusinessId == x.Id && o.IsActive && o.Service.IsActive && o.StaffMember.IsActive &&
                o.StaffMember.AcceptingBookings && o.BusinessLocation.IsActive &&
                (!request.MinPrice.HasValue || o.Price >= request.MinPrice) &&
                (!request.MaxPrice.HasValue || o.Price <= request.MaxPrice)));
        var total = await query.CountAsync(ct); var ids = await query.OrderBy(x => x.TradingName).Skip((page - 1) * size).Take(size).Select(x => x.Id).ToArrayAsync(ct);
        var businesses = await db.Businesses.AsNoTracking().Include(x => x.Locations).Where(x => ids.Contains(x.Id)).ToArrayAsync(ct);
        var ratings = await db.Reviews.AsNoTracking()
            .Where(x => ids.Contains(x.BusinessId) && x.ModerationStatus == ReviewModerationStatus.Active)
            .GroupBy(x => x.BusinessId)
            .Select(x => new { BusinessId = x.Key, Average = x.Average(review => review.Rating), Count = x.Count() })
            .ToDictionaryAsync(x => x.BusinessId, ct);
        var items = new List<BusinessSummaryDto>();
        foreach (var business in businesses.OrderBy(x => x.TradingName)) { var offerings = await db.ServiceOfferings.AsNoTracking().Where(o => o.Service.BusinessId == business.Id && o.IsActive && o.Service.IsActive && o.StaffMember.IsActive && o.StaffMember.AcceptingBookings && o.BusinessLocation.IsActive).Include(o => o.Service).ThenInclude(s => s.Category).ToArrayAsync(ct); var location = business.Locations.Single(x => x.IsPrimary && x.IsActive); var rating = ratings.GetValueOrDefault(business.Id); items.Add(new(business.Id, business.TradingName, business.Description, location.City, location.Suburb, location.Province, offerings.Min(x => x.Price), offerings.Select(x => x.Service.Category.Name).Distinct().ToArray(), (decimal)(rating?.Average ?? 0), rating?.Count ?? 0)); }
        return new(items, page, size, total);
    }

    public async Task<BusinessDetailDto?> DetailAsync(Guid id, CancellationToken ct)
    {
        var business = await db.Businesses.AsNoTracking().Include(x => x.Locations).Include(x => x.PortfolioImages).SingleOrDefaultAsync(x => x.Id == id && x.ApprovalStatus == BusinessApprovalStatus.Approved && x.Locations.Any(l => l.IsPrimary && l.IsActive), ct); if (business is null) return null;
        var services = await db.Services.AsNoTracking().Where(x => x.BusinessId == id && x.IsActive).Include(x => x.Category).Include(x => x.Offerings.Where(o => o.IsActive && o.StaffMember.IsActive && o.StaffMember.AcceptingBookings && o.BusinessLocation.IsActive)).ThenInclude(x => x.StaffMember).ToArrayAsync(ct);
        var location = business.Locations.Single(x => x.IsPrimary && x.IsActive);
        return new(business.Id, business.TradingName, business.Description, business.CancellationPolicy, business.PublicEmail, business.PublicPhone, location.AddressLine1, location.AddressLine2, location.Suburb, location.City, location.Province, location.PostalCode, services.Where(x => x.Offerings.Count != 0).Select(s => new PublicServiceDto(s.Id, s.Category.Name, s.Name, s.Description, s.Offerings.Select(o => new PublicOfferingDto(o.Id, o.StaffMemberId, o.StaffMember.DisplayName, o.Price, o.DurationMinutes, o.BufferMinutes)).ToArray())).ToArray(), business.PortfolioImages.Where(x => x.Status == PortfolioImageStatus.Active).OrderBy(x => x.DisplayOrder).Select(x => new PublicPortfolioImageDto(x.Id, x.ImageUrl, x.AltText)).ToArray());
    }
}
