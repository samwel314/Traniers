using ERP.Application.Modules.Academy.Players;
using ERP.Domain.Modules.Academy.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

/// <summary>
/// The EF Core side of <see cref="IPlayerRepository"/>. Registered automatically
/// by the naming convention in AddInfrastructure - no DI line was written for it.
///
/// The tenant and soft-delete filters are already applied by ApplicationDbContext,
/// so "does this code exist?" means "in this academy, among players who are not
/// deleted" without either condition being written here.
/// </summary>
internal sealed class PlayerRepository(ApplicationDbContext context)
    : Repository<Player>(context), IPlayerRepository
{
    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();

        return Set.AsNoTracking().AnyAsync(p => p.Code == normalized, cancellationToken);
    }

    public Task<bool> NationalIdExistsAsync(string nationalId, CancellationToken cancellationToken = default)
    {
        var normalized = nationalId.Trim();

        return Set.AsNoTracking().AnyAsync(p => p.NationalId == normalized, cancellationToken);
    }
}
