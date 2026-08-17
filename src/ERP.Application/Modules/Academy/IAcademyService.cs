using ERP.Application.Common.Models;
using ERP.Application.Modules.Academy.AcademyInput;
using ERP.Application.Modules.Academy.AcademyOutput;
using ERP.Domain.Common.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERP.Application.Modules.Academy
{
    public interface IAcademyService
    {
        Task<Result<AcademyResponseDto>> UpdateAcademyAsync(
            Guid id,
            UpdateAcademyRequest request,
            CancellationToken cancellation);

        Task<Result<AcademyResponseDto>> CreateAcademyAsync(
            CreateAcademyRequest request,
            CancellationToken cancellation);

        Task<Result<AcademyDto>> GetAcademyByIdAsync(
            Guid academyId,
            CancellationToken cancellation);

        Task<Result<PagedList<AcademyLockupDto>>> GetAllAcademiesAsync(
            int page = 1,
            int pageSize = 5,
            CancellationToken cancellation = default);

        Task<Result> DeleteAcademyAsync(
            Guid academyId,
            CancellationToken cancellation);
    }
}
