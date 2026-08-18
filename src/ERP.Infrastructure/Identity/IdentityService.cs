using ERP.Application.Common.Abstractions.Identity;
using ERP.Application.Common.Abstractions.Services;
using ERP.Application.Modules.User.Contracts;
using ERP.Domain.Common.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

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
        var user = await userManager.FindByNameAsync(userName);

        if (user is null)
            return Result.Failure<AuthTokens>(
                IdentityErrors.InvalidCredentials);

        if (!user.IsActive)
            return Result.Failure<AuthTokens>(
                IdentityErrors.UserInactive);

        var result = await signInManager.CheckPasswordSignInAsync(
            user,
            password,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
            return Result.Failure<AuthTokens>(
                IdentityErrors.UserLockedOut);

        if (!result.Succeeded)
            return Result.Failure<AuthTokens>(
                IdentityErrors.InvalidCredentials);

        return Result.Success(
            await IssueTokensAsync(user));
    }
    public async Task<Result<Guid>> RegisterAsync(
        string userName,
        string email,
        string password,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await userManager.FindByNameAsync(userName);

        if (existingUser is not null)
            return Result.Failure<Guid>(
                IdentityErrors.RegistrationFailed);

        var existingEmail = await userManager.FindByEmailAsync(email);

        if (existingEmail is not null)
            return Result.Failure<Guid>(
                IdentityErrors.RegistrationFailed);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            TenantId = tenantId,
            PreferredCulture = CultureInfo.InvariantCulture.Name,
            IsActive = true
        };

        var result = await userManager.CreateAsync(
            user,
            password);

        if (!result.Succeeded)
        {
            return Result.Failure<Guid>(
                Error.Validation(
                    "Identity.RegistrationFailed",
                    string.Join(
                        " ",
                        result.Errors.Select(e => e.Description))));
        }

        return Result.Success(user.Id);
    }

    public async Task<Result<AuthTokens>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {

        var user = await userManager.Users
            .FirstOrDefaultAsync(
                u => u.RefreshToken == refreshToken,
                cancellationToken);

        if (user is null)
            return Result.Failure<AuthTokens>(
                IdentityErrors.InvalidRefreshToken);

        if (!user.IsActive)
            return Result.Failure<AuthTokens>(
                IdentityErrors.UserInactive);

        if (user.RefreshTokenExpiresAtUtc is null ||
            user.RefreshTokenExpiresAtUtc <= dateTime.UtcNow)
        {
            return Result.Failure<AuthTokens>(
                IdentityErrors.InvalidRefreshToken);
        }

        return Result.Success(
            await IssueTokensAsync(user));
    }

    public async Task<Result> AssignRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure(IdentityErrors.UserNotFound);

        if (!await roleManager.RoleExistsAsync(role))
            return Result.Failure(IdentityErrors.RoleNotFound);

        var result = await userManager.AddToRoleAsync(user, role);

        return result.Succeeded
            ? Result.Success()
            : Result.Failure(Error.Failure("Identity.RoleAssignmentFailed",
                string.Join(" ", result.Errors.Select(e => e.Description))));
    }


public async Task<Result<AuthenticatedUser>> GetUserAsync(
    Guid userId,
    CancellationToken cancellationToken = default)
{
    var user = await userManager.FindByIdAsync(userId.ToString());

    if (user is null)
        return Result.Failure<AuthenticatedUser>(
            IdentityErrors.UserNotFound);

    var roles = await userManager.GetRolesAsync(user);

    var permissions = new HashSet<string>();

    foreach (var roleName in roles)
    {
        var role = await roleManager.FindByNameAsync(roleName);

        if (role is null)
            continue;

        var claims = await roleManager.GetClaimsAsync(role);

        foreach (var claim in claims)
        {
            if (claim.Type == ErpClaims.Permission)
                permissions.Add(claim.Value);
        }
    }

    return Result.Success(
        new AuthenticatedUser(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            [.. roles],
            [.. permissions]));
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

public async Task<Result<IReadOnlyList<UserDto>>> GetUsersAsync(
    CancellationToken cancellationToken = default)
{
    var users = await userManager.Users
        .AsNoTracking()
        .Select(u => new UserDto(
            u.Id,
            u.UserName ?? string.Empty,
            u.Email ?? string.Empty))
        .ToListAsync(cancellationToken);

    return Result.Success<IReadOnlyList<UserDto>>(users);
}

    public Task<bool> AnyUsersByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return userManager.Users.AnyAsync(u => u.Id == id , cancellationToken); 
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
