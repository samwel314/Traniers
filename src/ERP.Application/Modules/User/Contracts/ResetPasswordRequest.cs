namespace ERP.Application.Modules.User.Contracts
{
    public sealed record ResetPasswordRequest(
    Guid UserId,
    string Token,
    string NewPassword);
}
    
