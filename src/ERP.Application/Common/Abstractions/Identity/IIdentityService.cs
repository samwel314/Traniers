using ERP.Application.Modules.User.Contracts;
using ERP.Domain.Common.Results;

namespace ERP.Application.Common.Abstractions.Identity;

public sealed record AuthTokens(string AccessToken, DateTimeOffset ExpiresAtUtc, string RefreshToken);

public sealed record AuthenticatedUser(Guid UserId, string UserName, string Email, IReadOnlyList<string> Roles);

/// <summary>
/// ASP.NET Core Identity lives entirely in Infrastructure. The Application layer
/// only ever sees this contract, so swapping to Keycloak or Auth0 touches one project.
/// </summary>
public interface IIdentityService
{
    Task<Result<AuthTokens>> LoginAsync(string userName, string password, CancellationToken cancellationToken = default);

    Task<Result<Guid>> RegisterAsync(
        string userName,
        string email,
        string password,
        Guid tenantId,
        CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<UserDto>>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<Result<AuthTokens>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

  //  Task<Result> AssignRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);

    Task<Result<AuthenticatedUser>> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
