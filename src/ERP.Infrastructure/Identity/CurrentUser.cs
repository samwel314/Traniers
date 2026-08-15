using System.Security.Claims;
using ERP.Application.Common.Abstractions.Services;
using Microsoft.AspNetCore.Http;

namespace ERP.Infrastructure.Identity;

/// <summary>
/// Reads the caller from the current HTTP context.
///
/// This is why <c>ICurrentUser</c> is an Application abstraction: a background job
/// or a message consumer can supply its own implementation ("system" user, fixed
/// tenant) and every handler, filter and audit stamp keeps working unchanged.
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? UserName => Principal?.FindFirstValue(ClaimTypes.Name);

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public Guid? TenantId =>
        Guid.TryParse(Principal?.FindFirstValue(ErpClaims.TenantId), out var id) ? id : null;

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];

    public IReadOnlyList<string> Permissions =>
        Principal?.FindAll(ErpClaims.Permission).Select(c => c.Value).ToList() ?? [];

    public bool HasPermission(string permission) =>
        Principal?.HasClaim(ErpClaims.Permission, permission) ?? false;
}

/// <summary>
/// Stand-in for work that runs outside a request (seeding, cron jobs, consumers).
/// Register it in that host instead of <see cref="CurrentUser"/>.
/// </summary>
public sealed class SystemUser(Guid tenantId) : ICurrentUser
{
    public Guid? UserId => Guid.Empty;
    public string? UserName => "system";
    public string? Email => null;
    public Guid? TenantId { get; } = tenantId;
    public bool IsAuthenticated => true;
    public IReadOnlyList<string> Roles => ["System"];
    public IReadOnlyList<string> Permissions => [];
    public bool HasPermission(string permission) => true;
}
