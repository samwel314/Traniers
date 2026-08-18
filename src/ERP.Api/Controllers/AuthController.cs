using ERP.Application.Common.Abstractions.Identity;
using ERP.Application.Common.Abstractions.Services;
using ERP.Application.Modules.User.Contracts;
using ERP.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ERP.Api.Controllers;

public sealed class AuthController(
    IIdentityService identityService  , ICurrentUser currentUser) : ApiControllerBase
{
    /// <summary>
    /// Authenticates a user and returns access and refresh tokens.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AuthTokens), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await identityService.LoginAsync(
            request.UserName,
            request.Password,
            cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await identityService.RegisterAsync(
            request.UserName,
            request.Email,
            request.Password,
            request.TenantId,
            cancellationToken);

        return ToCreatedResult(result);
    }

    /// <summary>
    /// Refreshes an expired or expiring access token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AuthTokens), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await identityService.RefreshAsync(
            request.RefreshToken,
            cancellationToken);

        return ToActionResult(result);
    }
    [HttpGet("access")]
    [Authorize]
    public IActionResult GetAccess()
    {
        return Ok(new
        {
            UserId = currentUser.UserId,
            UserName = currentUser.UserName,
            Roles = currentUser.Roles,
            Permissions = currentUser.Permissions
        });
    }
    /// <summary>
    /// Gets the authenticated user's information.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(AuthenticatedUser), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(
        CancellationToken cancellationToken)
    {
        // هنجيب الـUserId من الـauthenticated principal.
        var userIdClaim = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userIdClaim is null ||
            !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        var result = await identityService.GetUserAsync(
            userId,
            cancellationToken);

        return ToActionResult(result);
    }

}