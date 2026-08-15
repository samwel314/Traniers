using ERP.Application.Common.Abstractions.Persistence;
using ERP.Domain.Modules.Academy.Entities;

namespace ERP.Application.Modules.Academy.Players;

/// <summary>
/// Extends the generic repository with what this module needs.
/// The implementation in Infrastructure is registered automatically by the
/// naming convention (IPlayerRepository -> PlayerRepository).
/// </summary>
public interface IPlayerRepository : IRepository<Player>
{
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> NationalIdExistsAsync(string nationalId, CancellationToken cancellationToken = default);
}
