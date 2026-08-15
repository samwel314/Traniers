using ERP.Application.Modules.Academy.Players;
using ERP.Application.Modules.Academy.Players.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

/// <summary>
/// Academy players.
///
/// The controller depends on IPlayerService and nothing else: no repository, no
/// DbContext, no entity. It calls one method and maps the Result.
/// </summary>
[Authorize]
public sealed class PlayersController(IPlayerService players) : ApiControllerBase
{
    /// <summary>Enrols a new player in the academy.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePlayer(
        CreatePlayerRequest request,
        CancellationToken cancellationToken)
        => ToCreatedResult(await players.CreateAsync(request, cancellationToken));
}
