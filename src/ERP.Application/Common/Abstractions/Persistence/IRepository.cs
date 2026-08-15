using System.Linq.Expressions;
using ERP.Domain.Common;

namespace ERP.Application.Common.Abstractions.Persistence;

/// <summary>
/// The database contract for one entity.
///
/// Note what is missing: no IQueryable, no DbSet, no SaveChanges. The Application
/// layer states what it needs; Infrastructure decides that EF Core provides it.
/// </summary>
public interface IRepository<TEntity> where TEntity : Entity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    void Add(TEntity entity);
    void Update(TEntity entity);

    /// <summary>Soft delete when the entity supports it, hard delete otherwise.</summary>
    void Remove(TEntity entity);
}

/// <summary>
/// One transaction per request, committed by UnitOfWorkBehavior - handlers
/// never call SaveChanges themselves.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
