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
        var useLocation = request.Latitude is >= -90 and <= 90 && request.Longitude is >= -180 and <= 180;
        var radiusKm = Math.Clamp(request.RadiusKm ?? 25, 1, 200);
        var candidates = await query.Select(x => new
        {
            x.Id,
            x.TradingName,
            Location = x.Locations.Where(l => l.IsPrimary && l.IsActive).Select(l => new { l.Latitude, l.Longitude }).Single()
        }).ToArrayAsync(ct);
        var ranked = candidates.Select(x => new
        {
            x.Id,
            x.TradingName,
            DistanceKm = useLocation && x.Location.Latitude.HasValue && x.Location.Longitude.HasValue
                ? DistanceKm(request.Latitude!.Value, request.Longitude!.Value, x.Location.Latitude.Value, x.Location.Longitude.Value)
                : (double?)null
        });
        if (useLocation) ranked = ranked.Where(x => x.DistanceKm.HasValue && x.DistanceKm <= radiusKm);
        var rankedArray = ranked.ToArray();
        var total = rankedArray.Length;
        var ids = rankedArray.Select(x => x.Id).ToArray();
        var distances = rankedArray.ToDictionary(x => x.Id, x => x.DistanceKm);
        var businesses = (await db.Businesses.AsNoTracking().Include(x => x.Locations).Include(x => x.PortfolioImages).Where(x => ids.Contains(x.Id)).ToArrayAsync(ct)).ToDictionary(x => x.Id);
        var ratings = await db.Reviews.AsNoTracking()
            .Where(x => ids.Contains(x.BusinessId) && x.ModerationStatus == ReviewModerationStatus.Active)
            .GroupBy(x => x.BusinessId)
            .Select(x => new { BusinessId = x.Key, Average = x.Average(review => review.Rating), Count = x.Count() })
            .ToDictionaryAsync(x => x.BusinessId, ct);
        var weeklyAvailability = await db.WeeklyAvailabilities.AsNoTracking()
            .Where(x => ids.Contains(x.StaffMember.BusinessId) && x.StaffMember.IsActive && x.StaffMember.AcceptingBookings)
            .Select(x => new { x.StaffMember.BusinessId, x.DayOfWeek })
            .ToListAsync(ct);
        var availabilityByBusiness = weeklyAvailability.GroupBy(x => x.BusinessId)
            .ToDictionary(x => x.Key, x => x.Select(item => item.DayOfWeek).Distinct().ToArray());
        var items = new List<BusinessSummaryDto>();
        foreach (var id in ids) { var business = businesses[id]; var offerings = await db.ServiceOfferings.AsNoTracking().Where(o => o.Service.BusinessId == business.Id && o.IsActive && o.Service.IsActive && o.StaffMember.IsActive && o.StaffMember.AcceptingBookings && o.BusinessLocation.IsActive).Include(o => o.Service).ThenInclude(s => s.Category).ToArrayAsync(ct); var location = business.Locations.Single(x => x.IsPrimary && x.IsActive); var rating = ratings.GetValueOrDefault(business.Id); var availableDays = availabilityByBusiness.GetValueOrDefault(business.Id, []); var coverImage = business.PortfolioImages.Where(x => x.Status == PortfolioImageStatus.Active).OrderBy(x => x.DisplayOrder).Select(x => x.ImageUrl).FirstOrDefault(); items.Add(new(business.Id, business.TradingName, business.Description, location.City, location.Suburb, location.Province, business.IsDemo, coverImage, offerings.Min(x => x.Price), offerings.Select(x => x.Service.Category.Name).Distinct().ToArray(), (decimal)(rating?.Average ?? 0), rating?.Count ?? 0, NextAvailableDate(availableDays), distances[id])); }
        var sort = request.Sort?.Trim().ToLowerInvariant();
        IEnumerable<BusinessSummaryDto> ordered = sort switch
        {
            "distance" when useLocation => items.OrderBy(x => x.DistanceKm ?? double.MaxValue).ThenBy(x => x.TradingName),
            "price" => items.OrderBy(x => x.FromPrice).ThenByDescending(x => x.AverageRating).ThenBy(x => x.TradingName),
            "rating" => items.OrderByDescending(x => x.AverageRating).ThenByDescending(x => x.ReviewCount).ThenBy(x => x.TradingName),
            "availability" => items.OrderBy(x => x.NextAvailableDate ?? DateOnly.MaxValue).ThenByDescending(x => x.AverageRating).ThenBy(x => x.TradingName),
            _ when useLocation => items.OrderBy(x => x.DistanceKm ?? double.MaxValue).ThenByDescending(x => x.AverageRating).ThenBy(x => x.TradingName),
            _ => items.OrderByDescending(x => x.AverageRating).ThenByDescending(x => x.ReviewCount).ThenBy(x => x.NextAvailableDate ?? DateOnly.MaxValue).ThenBy(x => x.TradingName)
        };
        return new(ordered.Skip((page - 1) * size).Take(size).ToArray(), page, size, total);
    }

    private static double DistanceKm(double fromLatitude, double fromLongitude, double toLatitude, double toLongitude)
    {
        const double earthRadiusKm = 6371.0088;
        static double Radians(double degrees) => degrees * Math.PI / 180;
        var latitudeDelta = Radians(toLatitude - fromLatitude);
        var longitudeDelta = Radians(toLongitude - fromLongitude);
        var a = Math.Pow(Math.Sin(latitudeDelta / 2), 2) + Math.Cos(Radians(fromLatitude)) * Math.Cos(Radians(toLatitude)) * Math.Pow(Math.Sin(longitudeDelta / 2), 2);
        return Math.Round(earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)), 1);
    }

    private static DateOnly? NextAvailableDate(IReadOnlyCollection<DayOfWeek> days)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var offset = 0; offset < 7; offset++)
        {
            var candidate = today.AddDays(offset);
            if (days.Contains(candidate.DayOfWeek)) return candidate;
        }
        return null;
    }

    public async Task<BusinessDetailDto?> DetailAsync(Guid id, CancellationToken ct)
    {
        var business = await db.Businesses.AsNoTracking().Include(x => x.Locations).Include(x => x.PortfolioImages).SingleOrDefaultAsync(x => x.Id == id && x.ApprovalStatus == BusinessApprovalStatus.Approved && x.Locations.Any(l => l.IsPrimary && l.IsActive), ct); if (business is null) return null;
        var services = await db.Services.AsNoTracking().Where(x => x.BusinessId == id && x.IsActive).Include(x => x.Category).Include(x => x.Offerings.Where(o => o.IsActive && o.StaffMember.IsActive && o.StaffMember.AcceptingBookings && o.BusinessLocation.IsActive)).ThenInclude(x => x.StaffMember).ToArrayAsync(ct);
        var staff = await db.StaffMembers.AsNoTracking().Where(x => x.BusinessId == id && x.IsActive).OrderBy(x => x.DisplayName).Select(x => new PublicStaffDto(x.Id, x.DisplayName, x.Biography)).ToArrayAsync(ct);
        var availability = await db.WeeklyAvailabilities.AsNoTracking().Where(x => x.StaffMember.BusinessId == id && x.StaffMember.IsActive && x.StaffMember.AcceptingBookings).Select(x => new { x.DayOfWeek, x.StartTime, x.EndTime }).ToArrayAsync(ct);
        var openingHours = availability.GroupBy(x => x.DayOfWeek).OrderBy(x => ((int)x.Key + 6) % 7).Select(x => new PublicOpeningHoursDto(x.Key.ToString(), x.Min(item => item.StartTime).ToString("HH:mm"), x.Max(item => item.EndTime).ToString("HH:mm"))).ToArray();
        var location = business.Locations.Single(x => x.IsPrimary && x.IsActive);
        return new(business.Id, business.TradingName, business.Description, business.CancellationPolicy, business.PublicEmail, business.PublicPhone, business.IsDemo, location.AddressLine1, location.AddressLine2, location.Suburb, location.City, location.Province, location.PostalCode, services.Where(x => x.Offerings.Count != 0).Select(s => new PublicServiceDto(s.Id, s.Category.Name, s.Name, s.Description, s.Offerings.Select(o => new PublicOfferingDto(o.Id, o.StaffMemberId, o.StaffMember.DisplayName, o.Price, o.DurationMinutes, o.BufferMinutes)).ToArray())).ToArray(), business.PortfolioImages.Where(x => x.Status == PortfolioImageStatus.Active).OrderBy(x => x.DisplayOrder).Select(x => new PublicPortfolioImageDto(x.Id, x.ImageUrl, x.AltText)).ToArray(), staff, openingHours);
    }
}
