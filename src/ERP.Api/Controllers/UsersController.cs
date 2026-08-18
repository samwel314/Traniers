using ERP.Application.Common.Abstractions.Identity;
using ERP.Application.Common.Abstractions.Services;
using ERP.Application.Common.Security;
using ERP.Application.Modules.User.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

[Authorize(Roles = Roles.Administrator)]
public sealed class UsersController(
    IIdentityService identityService ) : ApiControllerBase
{
    /// <summary>
    /// Gets all registered users.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await identityService.GetUsersAsync(
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AuthenticatedUser), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await identityService.GetUserAsync(
            id,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Assigns a role to an existing user.
    /// </summary>
    //[HttpPost("{id:guid}/roles")]
    //[Consumes("application/json")]
    //[ProducesResponseType(StatusCodes.Status204NoContent)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(StatusCodes.Status403Forbidden)]
    //[ProducesResponseType(StatusCodes.Status404NotFound)]
    //public async Task<IActionResult> AssignRole(
    //    Guid id,
    //    [FromBody] AssignRoleRequest request,
    //    CancellationToken cancellationToken)
    //{
    //    var result = await identityService.(
    //        id,
    //        request.Role,
    //        cancellationToken);

    //    return ToActionResult(result);
    //}
}
