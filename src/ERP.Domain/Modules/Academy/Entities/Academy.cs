using ERP.Domain.Common;

namespace ERP.Domain.Modules.Academy.Entities;

public class Academy : Entity, IAuditable, ISoftDeletable
{
    public int SerialNumber { get; set; }
    public string NameEn { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? LogoPath { get; set; }
    public Guid SportId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? ClubId { get; set; }
    public Guid? AdminId { get; set; }
    public Guid CityId { get; set; }
    public Guid AreaId { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Email { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Address { get; set; } = null!;
    public string FirstPhone { get; set; } = null!;
    public string? SecondPhone { get; set; }
    public string? FacebookUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public bool IsDeleted { get; set;  }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
}