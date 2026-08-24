using ERP.Application.Common.Abstractions.Persistence;
using ERP.Application.Common.Abstractions.Services;
using ERP.Application.Common.Models;
using ERP.Application.Common.Services;
using ERP.Application.Modules.Trainee.Mapper;
using ERP.Application.Modules.Trainee.TraineeInput;
using ERP.Application.Modules.Trainee.TraineeOutput;
using ERP.Domain.Common.Results;
using ERP.Domain.Modules.Trainee.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Application.Modules.Trainee
{
    public class TraineeService : ITraineeService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IImageService _imageService;
        private readonly IInputValidator _validator;
        private readonly ILogger<TraineeService> _logger;

        public TraineeService(
            IApplicationDbContext dbContext,
            IImageService imageService,
            IInputValidator validator,
            ILogger<TraineeService> logger)
        {
            _dbContext = dbContext;
            _imageService = imageService;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Result<TraineeLookupResponseDto>> GetByPhoneAsync(
            string phoneNumber,
            CancellationToken cancellation)
        {
            try
            {
                phoneNumber = phoneNumber?.Trim() ?? string.Empty;

                var trainee = await _dbContext.Trainees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.PhoneNumber == phoneNumber,
                        cancellation);

                if (trainee is null)
                {
                    return Result.Failure<TraineeLookupResponseDto>(
                        Error.NotFound(
                            "Trainee.NotFound",
                            "The requested trainee was not found."));
                }

                var response = new TraineeLookupResponseDto
                {
                    Id = trainee.Id,
                    FirstName = trainee.FirstName,
                    LastName = trainee.LastName,
                    PhoneNumber = trainee.PhoneNumber!,
                    Age = trainee.Age,
                    Gender = trainee.Gender,
                    Photo = trainee.Photo,
                    Type = trainee.Type
                };

                // Parent → get children
                if (trainee.Type == RegistrationType.Parent)
                {
                    response.Children = await _dbContext.Trainees
                        .AsNoTracking()
                        .Where(x => x.ParentId == trainee.Id)
                        .Select(x => new TraineeChildDto
                        {
                            Id = x.Id,
                            FirstName = x.FirstName,
                            LastName = x.LastName,
                            PhoneNumber = x.PhoneNumber!,
                            Age = x.Age,
                            Gender = x.Gender,
                            Photo = x.Photo
                        })
                        .ToListAsync(cancellation);
                }

                return Result.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to get trainee by phone number.");

                throw;
            }
        }

        public async Task<Result> AddExistingTraineesToAcademyAsync(
            AddExistingTraineesRequest request,
            CancellationToken cancellation)
        {
            try
            {
                // Validate request
                await _validator.ValidateAsync(
                    request,
                    cancellation);

                // Academy must exist
                var academyExists = await _dbContext.Academies
                    .AnyAsync(
                        x => x.Id == request.AcademyId &&
                             !x.IsDeleted,
                        cancellation);

                if (!academyExists)
                {
                    return Result.Failure(
                        Error.NotFound(
                            "Academy.NotFound",
                            "The requested academy was not found."));
                }

                // Remove duplicate IDs from request
                var traineeIds = request.TraineeIds
                    .Distinct()
                    .ToList();

                // Get existing trainees
                var trainees = await _dbContext.Trainees
                    .Where(x => traineeIds.Contains(x.Id))
                    .ToListAsync(cancellation);

                // Make sure every requested trainee exists
                if (trainees.Count != traineeIds.Count)
                {
                    return Result.Failure(
                        Error.NotFound(
                            "Trainee.NotFound",
                            "One or more trainees were not found."));
                }

                // Get already registered trainees
                var alreadyRegisteredIds =
                    await _dbContext.AcademyTrainees
                        .Where(x =>
                            x.AcademyId == request.AcademyId &&
                            traineeIds.Contains(x.TraineeId))
                        .Select(x => x.TraineeId)
                        .ToListAsync(cancellation);

                var newTraineeIds = traineeIds
                    .Except(alreadyRegisteredIds)
                    .ToList();

                // Add only new relationships
                foreach (var traineeId in newTraineeIds)
                {
                    await _dbContext.AcademyTrainees.AddAsync(
                        new AcademyTrainee
                        {
                            AcademyId = request.AcademyId,
                            TraineeId = traineeId,
                            Status = TraineeStatus.Waiting
                        },
                        cancellation);
                }

                await _dbContext.SaveChangesAsync(cancellation);

                return Result.Success();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to add existing trainees to academy {AcademyId}.",
                    request.AcademyId);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while adding trainees to academy {AcademyId}.",
                    request.AcademyId);

                throw;
            }
        }
        public async Task<Result<PagedList<AcademyTraineeDto>>> GetAcademyTraineesAsync(
    Guid academyId,
    TraineeStatus? status,
    int page = 1,
    int pageSize = 10,
    CancellationToken cancellation = default)
        {
            try
            {
                page = Math.Max(page, 1);
                pageSize = Math.Max(pageSize, 5);

                // Academy must exist
                var academyExists = await _dbContext.Academies
                    .AnyAsync(
                        x => x.Id == academyId &&
                             !x.IsDeleted,
                        cancellation);

                if (!academyExists)
                {
                    return Result.Failure<PagedList<AcademyTraineeDto>>(
                        Error.NotFound(
                            "Academy.NotFound",
                            "The requested academy was not found."));
                }

                var query = _dbContext.AcademyTrainees
                    .AsNoTracking()
                    .Where(x =>
                        x.AcademyId == academyId &&
                        !x.Trainee.IsDeleted);

                // Optional status filter
                if (status.HasValue)
                {
                    query = query.Where(x => x.Status == status.Value);
                }

                var totalCount = await query.CountAsync(cancellation);

                var items = await query
                    .OrderBy(x => x.Trainee.FirstName)
                    .ThenBy(x => x.Trainee.LastName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new AcademyTraineeDto
                    {
                        Id = x.Trainee.Id,

                        FirstName = x.Trainee.FirstName,

                        LastName = x.Trainee.LastName,

                        PhoneNumber = x.Trainee.PhoneNumber!,

                        Age = x.Trainee.Age,

                        Gender = x.Trainee.Gender,

                        Photo = x.Trainee.Photo,

                        Type = x.Trainee.Type,

                        Status = x.Status,

                        ParentId = x.Trainee.ParentId,

                        ParentName = x.Trainee.ParentId != null
                            ? x.Trainee.Parent!.FirstName + " " +
                              x.Trainee.Parent.LastName
                            : null
                    })
                    .ToListAsync(cancellation);

                var result = new PagedList<AcademyTraineeDto>(
                    items,
                    page,
                    pageSize,
                    totalCount);

                return Result.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to get trainees for academy {AcademyId}.",
                    academyId);

                throw;
            }
        }
        public async Task<Result<TraineeResponseDto>> CreateTraineeAsync(
            CreateTraineeRequest request,
            CancellationToken cancellation)
        {
            try
            {
                // Normalize input
                NormalizeRequest(request);

                // FluentValidation
                await _validator.ValidateAsync(
                    request,
                    cancellation);

                // Academy must exist
                var academyExists = await _dbContext.Academies
                    .AnyAsync(
                        x => x.Id == request.AcademyId &&
                             !x.IsDeleted,
                        cancellation);

                if (!academyExists)
                {
                    return Result.Failure<TraineeResponseDto>(
                        Error.NotFound(
                            "Academy.NotFound",
                            "The requested academy was not found."));
                }

                // This method is only for NEW registrations.
                // The phone number should already have been checked
                // through GetByPhoneAsync.
                var existingTrainee = await _dbContext.Trainees
                    .AnyAsync(
                        x => x.PhoneNumber == request.PhoneNumber,
                        cancellation);

                if (existingTrainee)
                {
                    return Result.Failure<TraineeResponseDto>(
                        Error.Conflict(
                            "Trainee.AlreadyExists",
                            "A trainee with this phone number already exists."));
                }

                // =========================================================
                // SELF
                // =========================================================

                if (request.Type == RegistrationType.Self)
                {
                    var trainee = request.ToEntity();

                    var photoResult = await HandlePhotoAsync(
                        request.Photo);

                    if (photoResult.IsFailure)
                    {
                        return Result.Failure<TraineeResponseDto>(
                            photoResult.Error);
                    }

                    trainee.Photo = photoResult.Value;

                    await _dbContext.Trainees.AddAsync(
                        trainee,
                        cancellation);

                    await _dbContext.AcademyTrainees.AddAsync(
                        new AcademyTrainee
                        {
                            AcademyId = request.AcademyId,
                            TraineeId = trainee.Id,
                            Status = TraineeStatus.Waiting
                        },
                        cancellation);

                    await _dbContext.SaveChangesAsync(
                        cancellation);

                    return Result.Success(
                        trainee.ToResponse());
                }

                // =========================================================
                // PARENT + CHILDREN
                // =========================================================

                var parent = request.ToEntity();

                parent.Type = RegistrationType.Parent;

                var parentPhotoResult = await HandlePhotoAsync(
                    request.Photo);

                if (parentPhotoResult.IsFailure)
                {
                    return Result.Failure<TraineeResponseDto>(
                        parentPhotoResult.Error);
                }

                parent.Photo = parentPhotoResult.Value;

                await _dbContext.Trainees.AddAsync(
                    parent,
                    cancellation);

                foreach (var childRequest in request.Children)
                {
                    var child = childRequest.ToEntity(
                        parent.Id);

                    var childPhotoResult = await HandlePhotoAsync(
                        childRequest.Photo);

                    if (childPhotoResult.IsFailure)
                    {
                        return Result.Failure<TraineeResponseDto>(
                            childPhotoResult.Error);
                    }

                    child.Photo = childPhotoResult.Value;

                    await _dbContext.Trainees.AddAsync(
                        child,
                        cancellation);

                    await _dbContext.AcademyTrainees.AddAsync(
                        new AcademyTrainee
                        {
                            AcademyId = request.AcademyId,
                            TraineeId = child.Id,
                            Status = TraineeStatus.Waiting
                        },
                        cancellation);
                }

                await _dbContext.SaveChangesAsync(
                    cancellation);

                return Result.Success(
                    parent.ToResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while creating trainee registration.");

                throw;
            }
        }
        private static void NormalizeRequest(CreateTraineeRequest request)
        {
            request.FirstName = request.FirstName?.Trim() ?? string.Empty;
            request.LastName = request.LastName?.Trim() ?? string.Empty;
            request.PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;

            foreach (var child in request.Children)
            {
                child.FirstName = child.FirstName?.Trim() ?? string.Empty;
                child.LastName = child.LastName?.Trim() ?? string.Empty;
                child.PhoneNumber = child.PhoneNumber?.Trim() ?? string.Empty;
            }
        }
        private async Task<Result<string?>> HandlePhotoAsync(IFormFile? file)
        {
            if (file is null)
                return Result.Success<string?>(null);

            if (!await _imageService.IsValidImageAsync(file))
            {
                return Result.Failure<string?>(
                    Error.Validation(
                        "Trainee.InvalidPhoto",
                        "The trainee photo is invalid."));
            }

            var path = await _imageService.SaveImageAsync(
                file,
                "Trainees");

            return Result.Success<string?>(path);
        }

    }


}

    
