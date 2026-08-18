
namespace ERP.Application.Modules.User.Contracts
{
    public record RegisterRequest(string UserName,
            string Email,
            string Password,
            Guid TenantId);

    public record AssignRoleRequest(Guid UserId, string Role);  
}
