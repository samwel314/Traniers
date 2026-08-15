using ERP.Application.Common.Abstractions.Services;
using ERP.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ERP.Infrastructure.Persistence.Interceptors;
// before saving
/// <summary>
/// Stamps who/when on every save, turns deletes into soft deletes, and assigns the
/// tenant on insert. Because it runs in the interceptor, no handler can forget it
/// and no audit column is ever set by hand.
/// </summary>
public sealed class AuditableEntityInterceptor(
    ICurrentUser currentUser,
    IDateTimeProvider dateTime) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null) return;

        var now = dateTime.UtcNow;
        var user = currentUser.UserName ?? currentUser.UserId?.ToString() ?? "system";

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is IHasTenant tenanted &&
                entry.State == EntityState.Added &&
                tenanted.TenantId == Guid.Empty &&
                currentUser.TenantId is { } tenantId)
            {
                tenanted.TenantId = tenantId;
            }

            if (entry.Entity is IAuditable auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditable.CreatedAtUtc = now;
                        auditable.CreatedBy = user;
                        break;

                    case EntityState.Modified or EntityState.Unchanged when HasChangedOwnedEntities(entry):
                        auditable.ModifiedAtUtc = now;
                        auditable.ModifiedBy = user;
                        break;
                }
            }

            // A DELETE on an ERP table is almost always a mistake. Flag it instead.
            if (entry is { State: EntityState.Deleted, Entity: ISoftDeletable deletable })
            {
                entry.State = EntityState.Modified;
                deletable.IsDeleted = true;
                deletable.DeletedAtUtc = now;
                deletable.DeletedBy = user;
            }
        }
    }

    /// <summary>An edit to an owned value object counts as an edit to its owner.</summary>
    private static bool HasChangedOwnedEntities(EntityEntry entry)
        => entry.State == EntityState.Modified ||
           entry.References.Any(r =>
               r.TargetEntry is { } target &&
               target.Metadata.IsOwned() &&
               target.State is EntityState.Added or EntityState.Modified);
}
