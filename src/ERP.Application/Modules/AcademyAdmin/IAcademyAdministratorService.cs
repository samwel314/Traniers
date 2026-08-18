using ERP.Application.Modules.Academy.AcademyOutput;
using ERP.Domain.Common.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERP.Application.Modules.AcademyAdmin
{
    public interface IAcademyAdministratorService
    {
        Task<Result> AssignAsync(
        Guid academyId,
        Guid userId,
        CancellationToken cancellationToken = default);

        Task<Result> RemoveAsync(
            Guid academyId,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<Result<IReadOnlyList<AcademyLockupDto>>> GetMyAcademiesAsync(
            CancellationToken cancellationToken = default);
    }
}
