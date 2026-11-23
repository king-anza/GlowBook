using GlowBook.Domain.Professionals;

namespace GlowBook.Domain.Catalogue;

public sealed class ServiceCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Service
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public Business Business { get; set; } = null!;
    public ServiceCategory Category { get; set; } = null!;
    public ICollection<ServiceOffering> Offerings { get; set; } = [];
}

public sealed class ServiceOffering
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public Guid StaffMemberId { get; set; }
    public Guid BusinessLocationId { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public int BufferMinutes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public Service Service { get; set; } = null!;
    public StaffMember StaffMember { get; set; } = null!;
    public BusinessLocation BusinessLocation { get; set; } = null!;
}
