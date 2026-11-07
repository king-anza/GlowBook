using System.ComponentModel.DataAnnotations;

namespace GlowBook.Application.Catalogue;

public sealed record CategoryDto(Guid Id, string Name, string Slug);
public sealed record ServiceRequest(Guid CategoryId, [property: Required, MinLength(2), MaxLength(150)] string Name, [property: MaxLength(2000)] string Description, Guid? ConcurrencyToken);
public sealed record OfferingRequest(Guid StaffMemberId, [property: Range(0, 999999.99)] decimal Price, [property: Range(5, 720)] int DurationMinutes, [property: Range(0, 180)] int BufferMinutes, Guid? ConcurrencyToken);
public sealed record OfferingDto(Guid Id, Guid StaffMemberId, string StaffName, decimal Price, int DurationMinutes, int BufferMinutes, bool IsActive, Guid ConcurrencyToken);
public sealed record ServiceDto(Guid Id, Guid CategoryId, string CategoryName, string Name, string Description, bool IsActive, Guid ConcurrencyToken, IReadOnlyCollection<OfferingDto> Offerings);

public interface ICatalogueService
{
    Task<IReadOnlyCollection<CategoryDto>> CategoriesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<ServiceDto>> ServicesAsync(Guid ownerId, CancellationToken ct);
    Task<ServiceDto?> SaveServiceAsync(Guid ownerId, Guid? serviceId, ServiceRequest request, CancellationToken ct);
    Task<OfferingDto?> SaveOfferingAsync(Guid ownerId, Guid serviceId, Guid? offeringId, OfferingRequest request, CancellationToken ct);
    Task<bool> SetServiceActiveAsync(Guid ownerId, Guid serviceId, bool active, CancellationToken ct);
    Task<bool> SetOfferingActiveAsync(Guid ownerId, Guid offeringId, bool active, CancellationToken ct);
}
