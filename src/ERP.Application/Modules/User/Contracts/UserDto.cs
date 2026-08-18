using System;
using System.Collections.Generic;
using System.Text;

namespace ERP.Application.Modules.User.Contracts
{
    public sealed record UserDto(
        Guid Id,
        string UserName,
        string Email);
}
    
