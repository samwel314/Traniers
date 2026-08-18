using System;
using System.Collections.Generic;
using System.Text;

namespace ERP.Application.Modules.AcademyAdmin.Contracts
{
    public record AssignAcademyAdministratorRequest(Guid AcademyId, Guid UserId); 
}
