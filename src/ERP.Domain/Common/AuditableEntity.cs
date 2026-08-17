namespace ERP.Domain.Common;

/// <summary>
/// The base every table inherits: who created it, who changed it, is it deleted,
/// and which company owns it.
///
/// You never set these by hand - AuditableEntityInterceptor fills them in on save.
/// </summary>
public abstract class AuditableEntity : Entity, IAuditable, ISoftDeletable, IHasTenant
{
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public Guid TenantId { get; set; }
}
