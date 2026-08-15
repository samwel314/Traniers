using ERP.Application.Common.Abstractions.Services;
using ERP.Application.Common.Exceptions;

namespace ERP.Application.Common.Services;

/// <summary>
/// Permission checks used to be an attribute read by a pipeline behavior.
/// In a service layer they are an explicit first line instead:
///
/// <code>guard.Require(Permissions.Inventory.CreateProduct);</code>
///
/// Keeping it in the service rather than on the controller means the rule still
/// holds when a background job or a message consumer calls the same method.
/// </summary>
public interface IPermissionGuard
{
    /// <summary>Throws <see cref="ForbiddenAccessException"/> when the caller lacks the permission.</summary>
    void Require(string permission);

    /// <summary>Non-throwing variant, for branching rather than refusing.</summary>
    bool Has(string permission);
}

internal sealed class PermissionGuard(ICurrentUser currentUser) : IPermissionGuard
{
    public void Require(string permission)
    {
        if (!currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("Authentication is required for this operation.");

        if (!currentUser.HasPermission(permission))
            throw new ForbiddenAccessException(permission);
    }

    public bool Has(string permission) => currentUser.HasPermission(permission);
}
