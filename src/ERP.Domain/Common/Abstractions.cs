namespace ERP.Domain.Common;

/// <summary>Rows are stamped by the persistence interceptor, never by hand.</summary>
public interface IAuditable
{
    DateTimeOffset CreatedAtUtc { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? ModifiedAtUtc { get; set; }
    string? ModifiedBy { get; set; }
}

/// <summary>Deleting is a flag, not a DELETE. ERP data is rarely truly removable.</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAtUtc { get; set; }
    string? DeletedBy { get; set; }
}

/// <summary>Multi-company / multi-branch isolation, enforced by a global query filter.</summary>
public interface IHasTenant
{
    Guid TenantId { get; set; }
}
