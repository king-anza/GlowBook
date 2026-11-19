namespace GlowBook.Application.Discovery;

public sealed record DiscoveryQuery(string? Search, Guid? CategoryId, string? Province, string? City, string? Suburb, decimal? MinPrice, decimal? MaxPrice, int Page = 1, int PageSize = 20);
public sealed record BusinessSummaryDto(Guid Id, string TradingName, string Description, string City, string Suburb, string Province, decimal FromPrice, IReadOnlyCollection<string> Categories, decimal AverageRating, int ReviewCount, DateOnly? NextAvailableDate);
public sealed record PublicOfferingDto(Guid Id, Guid StaffId, string StaffName, decimal Price, int DurationMinutes, int BufferMinutes);
public sealed record PublicServiceDto(Guid Id, string Category, string Name, string Description, IReadOnlyCollection<PublicOfferingDto> Offerings);
public sealed record PublicPortfolioImageDto(Guid Id, string ImageUrl, string AltText);
public sealed record BusinessDetailDto(Guid Id, string TradingName, string Description, string CancellationPolicy, string? PublicEmail, string? PublicPhone, string AddressLine1, string? AddressLine2, string Suburb, string City, string Province, string PostalCode, IReadOnlyCollection<PublicServiceDto> Services, IReadOnlyCollection<PublicPortfolioImageDto> PortfolioImages);
public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);

public interface IDiscoveryService
{
    Task<PagedResult<BusinessSummaryDto>> SearchAsync(DiscoveryQuery query, CancellationToken ct);
    Task<BusinessDetailDto?> DetailAsync(Guid businessId, CancellationToken ct);
}
