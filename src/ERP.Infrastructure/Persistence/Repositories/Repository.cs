using System.Linq.Expressions;
using ERP.Application.Common.Abstractions.Persistence;
using ERP.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>
/// The default EF Core implementation of <see cref="IRepository{TEntity}"/>.
/// Registered as an open generic, so a new entity gets a working repository
/// with no code at all. Derive from it when a module needs custom queries.
/// </summary>
public class Repository<TEntity>(ApplicationDbContext context) : IRepository<TEntity>
    where TEntity : Entity
{
    protected ApplicationDbContext Context { get; } = context;

    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    /// <summary>Read-only queries skip change tracking - measurably cheaper on ERP list screens.</summary>
    protected IQueryable<TEntity> Query => Set.AsNoTracking();

    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Set.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public virtual async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await Set.AsNoTracking().AnyAsync(predicate, cancellationToken);

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = Query;
        if (predicate is not null)
            query = query.Where(predicate);

        return await query.ToListAsync(cancellationToken);
    }

    public virtual void Add(TEntity entity) => Set.Add(entity);

    public virtual void Update(TEntity entity) => Set.Update(entity);

    /// <summary>
    /// Soft delete is handled by the audit interceptor: this marks the row Deleted
    /// and the interceptor rewrites it to an update when the entity supports it.
    /// </summary>
    public virtual void Remove(TEntity entity) => Set.Remove(entity);
}
