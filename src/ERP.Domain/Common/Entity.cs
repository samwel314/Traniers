namespace ERP.Domain.Common;

/// <summary>
/// Base class for every entity: it just carries the Id.
/// Two entities are the same when their Id is the same.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
