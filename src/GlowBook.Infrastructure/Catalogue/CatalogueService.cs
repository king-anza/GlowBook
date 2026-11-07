using GlowBook.Application.Catalogue;
using GlowBook.Domain.Catalogue;
using GlowBook.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GlowBook.Infrastructure.Catalogue;

internal sealed class CatalogueService(GlowBookDbContext db) : ICatalogueService
{
    public async Task<IReadOnlyCollection<CategoryDto>> CategoriesAsync(CancellationToken ct) => await db.ServiceCategories.Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).Select(x => new CategoryDto(x.Id, x.Name, x.Slug)).ToArrayAsync(ct);
    public async Task<IReadOnlyCollection<ServiceDto>> ServicesAsync(Guid ownerId, CancellationToken ct) => (await Query().Where(x => x.Business.OwnerUserId == ownerId).OrderBy(x => x.Name).Take(500).ToArrayAsync(ct)).Select(Map).ToArray();
    public async Task<ServiceDto?> SaveServiceAsync(Guid ownerId, Guid? id, ServiceRequest request, CancellationToken ct)
    {
        if (!await db.ServiceCategories.AnyAsync(x => x.Id == request.CategoryId && x.IsActive, ct)) return null;
        var business = await db.Businesses.SingleOrDefaultAsync(x => x.OwnerUserId == ownerId, ct); if (business is null) return null;
        Service service;
        if (id is null) { service = new Service { Id = Guid.NewGuid(), BusinessId = business.Id, CreatedAtUtc = DateTime.UtcNow }; db.Services.Add(service); }
        else { service = await Query().SingleOrDefaultAsync(x => x.Id == id && x.BusinessId == business.Id, ct) ?? throw new KeyNotFoundException(); if (service.ConcurrencyToken != request.ConcurrencyToken) throw new DbUpdateConcurrencyException(); }
        service.CategoryId = request.CategoryId; service.Name = request.Name.Trim(); service.Description = request.Description?.Trim() ?? string.Empty; service.UpdatedAtUtc = DateTime.UtcNow; service.ConcurrencyToken = Guid.NewGuid(); await db.SaveChangesAsync(ct); return Map(await Query().SingleAsync(x => x.Id == service.Id, ct));
    }
    public async Task<OfferingDto?> SaveOfferingAsync(Guid ownerId, Guid serviceId, Guid? id, OfferingRequest request, CancellationToken ct)
    {
        if (request.DurationMinutes % 5 != 0 || request.BufferMinutes % 5 != 0) return null;
        var service = await db.Services.Include(x => x.Business).SingleOrDefaultAsync(x => x.Id == serviceId && x.Business.OwnerUserId == ownerId, ct); if (service is null) return null;
        var staff = await db.StaffMembers.SingleOrDefaultAsync(x => x.Id == request.StaffMemberId && x.BusinessId == service.BusinessId && x.IsActive, ct); if (staff is null) return null;
        var location = await db.BusinessLocations.SingleAsync(x => x.BusinessId == service.BusinessId && x.IsPrimary && x.IsActive, ct);
        ServiceOffering offering;
        if (id is null) { offering = new ServiceOffering { Id = Guid.NewGuid(), ServiceId = serviceId, CreatedAtUtc = DateTime.UtcNow }; db.ServiceOfferings.Add(offering); }
        else { offering = await db.ServiceOfferings.SingleOrDefaultAsync(x => x.Id == id && x.ServiceId == serviceId, ct) ?? throw new KeyNotFoundException(); if (offering.ConcurrencyToken != request.ConcurrencyToken) throw new DbUpdateConcurrencyException(); }
        offering.StaffMemberId = staff.Id; offering.BusinessLocationId = location.Id; offering.Price = request.Price; offering.DurationMinutes = request.DurationMinutes; offering.BufferMinutes = request.BufferMinutes; offering.UpdatedAtUtc = DateTime.UtcNow; offering.ConcurrencyToken = Guid.NewGuid(); await db.SaveChangesAsync(ct); return new(offering.Id, staff.Id, staff.DisplayName, offering.Price, offering.DurationMinutes, offering.BufferMinutes, offering.IsActive, offering.ConcurrencyToken);
    }
    public async Task<bool> SetServiceActiveAsync(Guid ownerId, Guid id, bool active, CancellationToken ct) { var s = await Query().SingleOrDefaultAsync(x => x.Id == id && x.Business.OwnerUserId == ownerId, ct); if (s is null || active && !s.Offerings.Any(x => x.IsActive)) return false; s.IsActive = active; await db.SaveChangesAsync(ct); return true; }
    public async Task<bool> SetOfferingActiveAsync(Guid ownerId, Guid id, bool active, CancellationToken ct) { var o = await db.ServiceOfferings.Include(x => x.Service).ThenInclude(x => x.Business).SingleOrDefaultAsync(x => x.Id == id && x.Service.Business.OwnerUserId == ownerId, ct); if (o is null) return false; o.IsActive = active; await db.SaveChangesAsync(ct); return true; }
    private IQueryable<Service> Query() => db.Services.Include(x => x.Category).Include(x => x.Business).Include(x => x.Offerings).ThenInclude(x => x.StaffMember);
    private static ServiceDto Map(Service s) => new(s.Id, s.CategoryId, s.Category.Name, s.Name, s.Description, s.IsActive, s.ConcurrencyToken, s.Offerings.Select(o => new OfferingDto(o.Id, o.StaffMemberId, o.StaffMember.DisplayName, o.Price, o.DurationMinutes, o.BufferMinutes, o.IsActive, o.ConcurrencyToken)).ToArray());
}
