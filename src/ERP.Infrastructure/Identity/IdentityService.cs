using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ERP.Application.Common.Abstractions.Identity;
using ERP.Application.Common.Abstractions.Services;
using ERP.Domain.Common.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ERP.Infrastructure.Identity;

/// <summary>
/// The only implementation of <see cref="IIdentityService"/>. Everything
/// ASP.NET Identity related is sealed inside this file.
/// </summary>
public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    RoleManager<ApplicationRole> roleManager,
    IOptions<JwtOptions> jwtOptions,
    IDateTimeProvider dateTime) : IIdentityService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<Result<AuthTokens>> LoginAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
       

        return Result.Success(await IssueTokensAsync(null));
    }

    public async Task<Result<Guid>> RegisterAsync(
        string userName,
        string email,
        string password,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        
        return Result.Success(new Guid());
    }

    public async Task<Result<AuthTokens>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
      
        return Result.Success(await IssueTokensAsync(null));
    }

    //public async Task<Result> AssignRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    //{
    //    var user = await userManager.FindByIdAsync(userId.ToString());
    //    if (user is null)
    //        return Result.Failure(IdentityErrors.UserNotFound);

    //    if (!await roleManager.RoleExistsAsync(role))
    //        return Result.Failure(IdentityErrors.RoleNotFound);

    //    var result = await userManager.AddToRoleAsync(user, role);

    //    return result.Succeeded
    //        ? Result.Success()
    //        : Result.Failure(Error.Failure("Identity.RoleAssignmentFailed",
    //            string.Join(" ", result.Errors.Select(e => e.Description))));
    //}

    // Stub so the class still satisfies IIdentityService while the body above is
    // commented out. Fill it in, or drop AssignRoleAsync from the interface.
    public Task<Result> AssignRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success());

    public async Task<Result<AuthenticatedUser>> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure<AuthenticatedUser>(IdentityErrors.UserNotFound);

        var roles = await userManager.GetRolesAsync(user);

        return Result.Success(new AuthenticatedUser(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            [.. roles]));
    }

    private async Task<AuthTokens> IssueTokensAsync(ApplicationUser user)
    {
        var expiresAt = dateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ErpClaims.TenantId, user.TenantId.ToString()),
            new(ErpClaims.Culture, user.PreferredCulture)
        };

        foreach (var role in await userManager.GetRolesAsync(user))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));

            // Permissions are stored as role claims and flattened into the token,
            // so authorization never needs a database round trip per request.
            var appRole = await roleManager.FindByNameAsync(role);
            if (appRole is null) continue;

            foreach (var claim in await roleManager.GetClaimsAsync(appRole))
            {
                if (claim.Type == ErpClaims.Permission)
                    claims.Add(claim);
            }
        }

        claims.AddRange(await userManager.GetClaimsAsync(user));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims.DistinctBy(c => (c.Type, c.Value)),
            notBefore: dateTime.UtcNow.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAtUtc = dateTime.UtcNow.AddDays(_jwt.RefreshTokenDays);
        await userManager.UpdateAsync(user);

        return new AuthTokens(accessToken, expiresAt, refreshToken);
    }
}

/// <summary>Error codes mirrored by the Identity.* keys in ar.json / en.json.</summary>
public static class IdentityErrors
{
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Identity.InvalidCredentials", "Incorrect user name or password.");

    public static readonly Error UserLockedOut =
        Error.Forbidden("Identity.UserLockedOut", "The account is locked.");

    public static readonly Error UserInactive =
        Error.Forbidden("Identity.UserInactive", "This account is disabled.");

    public static readonly Error UserNotFound =
        Error.NotFound("Identity.UserNotFound", "User not found.");

    public static readonly Error RoleNotFound =
        Error.NotFound("Identity.RoleNotFound", "Role not found.");

    public static readonly Error RegistrationFailed =
        Error.Validation("Identity.RegistrationFailed", "Registration failed.");

    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("Identity.InvalidRefreshToken", "The session has expired.");
}
