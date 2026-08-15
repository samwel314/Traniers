using ERP.Application.Modules.Academy.Players.Contracts;
using ERP.Domain.Common.Results;

namespace ERP.Application.Modules.Academy.Players;

/// <summary>
/// The Players module's use cases. Only enrolment is implemented for now -
/// add GetById / Update / Deactivate here as you need them.
/// </summary>
public interface IPlayerService
{
    /// <summary>Enrols a new player and returns their new Id.</summary>
    Task<Result<Guid>> CreateAsync(CreatePlayerRequest request, CancellationToken cancellationToken = default);
}
