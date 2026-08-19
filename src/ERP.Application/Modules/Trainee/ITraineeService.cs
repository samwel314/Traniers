using ERP.Application.Common.Models;
using ERP.Application.Modules.Trainee.TraineeInput;
using ERP.Application.Modules.Trainee.TraineeOutput;
using ERP.Domain.Common.Results;
using ERP.Domain.Modules.Trainee.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERP.Application.Modules.Trainee
{
    public interface ITraineeService
    {
        Task<Result<TraineeLookupResponseDto>> GetByPhoneAsync(
            string phoneNumber,
            CancellationToken cancellation);

        Task<Result> AddExistingTraineesToAcademyAsync(
            AddExistingTraineesRequest request,
            CancellationToken cancellation);

        Task<Result<TraineeResponseDto>> CreateTraineeAsync(
            CreateTraineeRequest request,
            CancellationToken cancellation);
        Task<Result<PagedList<AcademyTraineeDto>>> GetAcademyTraineesAsync(
    Guid academyId,
    TraineeStatus? status,
    int page = 1,
    int pageSize = 10,
    CancellationToken cancellation = default);
    }
}