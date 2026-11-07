using System.ComponentModel.DataAnnotations;
using GlowBook.Domain.Professionals;

namespace GlowBook.Application.Professionals;

public sealed record LocationRequest([property: Required, MaxLength(150)] string AddressLine1, string? AddressLine2, [property: Required, MaxLength(100)] string Suburb, [property: Required, MaxLength(100)] string City, [property: Required, RegularExpression("^(Eastern Cape|Free State|Gauteng|KwaZulu-Natal|Limpopo|Mpumalanga|North West|Northern Cape|Western Cape)$")] string Province, [property: Required, RegularExpression("^[0-9]{4}$")] string PostalCode);
public sealed record BusinessRequest([property: Required, MaxLength(150)] string TradingName, [property: MaxLength(2000)] string Description, [property: MaxLength(2000)] string CancellationPolicy, [property: EmailAddress] string? PublicEmail, [property: Phone] string? PublicPhone, [property: Required] LocationRequest Location, Guid? ConcurrencyToken);
public sealed record StaffRequest([property: Required, MaxLength(100)] string DisplayName, [property: MaxLength(1000)] string Biography, bool AcceptingBookings = true);
public sealed record ReviewRequest([property: MaxLength(1000)] string? Reason);
public sealed record BusinessDto(Guid Id, string TradingName, string Description, string CancellationPolicy, string? PublicEmail, string? PublicPhone, BusinessApprovalStatus Status, string? ReviewReason, Guid ConcurrencyToken, LocationRequest Location, IReadOnlyCollection<StaffDto> Staff);
public sealed record StaffDto(Guid Id, string DisplayName, string Biography, bool IsActive, bool AcceptingBookings);
public sealed record PortfolioImageRequest([property: Required, Url, RegularExpression("^https://.*", ErrorMessage = "Image URL must use HTTPS.")] string ImageUrl, [property: Required, MaxLength(200)] string AltText, [property: Range(0, 9)] int DisplayOrder);
public sealed record PortfolioImageDto(Guid Id, string ImageUrl, string AltText, int DisplayOrder, PortfolioImageStatus Status);
public sealed record AdminPortfolioImageDto(Guid Id, string BusinessName, string ImageUrl, string AltText, PortfolioImageStatus Status, DateTime CreatedAtUtc);

public interface IProfessionalService
{
    Task<BusinessDto?> GetAsync(Guid ownerId, CancellationToken ct);
    Task<BusinessDto> SaveAsync(Guid ownerId, string ownerName, BusinessRequest request, CancellationToken ct);
    Task<bool> SubmitAsync(Guid ownerId, CancellationToken ct);
    Task<StaffDto?> AddStaffAsync(Guid ownerId, StaffRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<BusinessDto>> PendingAsync(CancellationToken ct);
    Task<bool> ReviewAsync(Guid actorId, Guid businessId, BusinessApprovalStatus status, string? reason, CancellationToken ct);
    Task<PortfolioImageDto?> AddPortfolioImageAsync(Guid ownerId, PortfolioImageRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<PortfolioImageDto>> PortfolioImagesAsync(Guid ownerId, CancellationToken ct);
    Task<bool> DeletePortfolioImageAsync(Guid ownerId, Guid imageId, CancellationToken ct);
    Task<bool> ModeratePortfolioImageAsync(Guid actorId, Guid imageId, bool hidden, CancellationToken ct);
    Task<IReadOnlyCollection<AdminPortfolioImageDto>> PortfolioModerationAsync(CancellationToken ct);
}
