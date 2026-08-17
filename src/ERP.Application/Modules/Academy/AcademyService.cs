using ERP.Application.Common.Abstractions.Persistence;
using ERP.Application.Common.Abstractions.Services;
using ERP.Application.Common.Models;
using ERP.Application.Common.Services;
using ERP.Application.Modules.Academy.AcademyInput;
using ERP.Application.Modules.Academy.AcademyMapper;
using ERP.Application.Modules.Academy.AcademyOutput;
using ERP.Domain.Common.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace ERP.Application.Modules.Academy
{

    public class AcademyService : IAcademyService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IImageService _imageService;
        private readonly IInputValidator _validator;
        private readonly ILogger<AcademyService> _logger;

        public AcademyService(
            IApplicationDbContext dbContext,
            IImageService imageService,
            IInputValidator validator,
            ILogger<AcademyService> logger)
        {
            _dbContext = dbContext;
            _imageService = imageService;
            _validator = validator;
            _logger = logger;
        }

        public async Task<Result<AcademyResponseDto>> CreateAcademyAsync(
            CreateAcademyRequest request,
            CancellationToken cancellation)
        {
            try
            {
                // Normalize input
                request.NameAr = request.NameAr?.Trim() ?? string.Empty;
                request.NameEn = request.NameEn?.Trim() ?? string.Empty;
                request.DescriptionAr = request.DescriptionAr?.Trim();
                request.DescriptionEn = request.DescriptionEn?.Trim();

                // FluentValidation
                await _validator.ValidateAsync(request, cancellation);

                // Business rule: academy name must be unique
                var isNameExist = await _dbContext.Academies.AnyAsync(
                    a => !a.IsDeleted &&
                         (a.NameEn == request.NameEn ||
                          a.NameAr == request.NameAr),
                    cancellation);

                if (isNameExist)
                {
                    return Result.Failure<AcademyResponseDto>(
                        Error.Conflict(
                            "Academy.NameAlreadyExists",
                            "Academy name already exists."));
                }

                // Business rule: logo must be a valid image
                if (request.Logo is not null &&
                    !_imageService.IsValidImage(request.Logo))
                {
                    return Result.Failure<AcademyResponseDto>(
                        Error.Validation(
                            "Academy.InvalidLogo",
                            "The provided logo is invalid."));
                }

                // Map request -> entity
                var academy = request.ToEntity();

                // Save logo
                if (request.Logo is not null)
                {
                    academy.LogoPath = await _imageService.SaveImageAsync(
                        request.Logo,
                        "Logos");
                }

                await _dbContext.Academies.AddAsync(
                    academy,
                    cancellation);

                await _dbContext.SaveChangesAsync(cancellation);

                return Result.Success(
                    academy.ToResponse());
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to create academy.");

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while creating academy.");

                throw;
            }
        }

        public async Task<Result<AcademyDto>> GetAcademyByIdAsync(
            Guid academyId,
            CancellationToken cancellation)
        {
            try
            {
                var isEnglish =
                    CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "en";

                var academy = await _dbContext.Academies
                    .AsNoTracking()
                    .Where(a => a.Id == academyId && !a.IsDeleted)
                    .Select(a => new AcademyDto
                    {
                        Id = a.Id,

                        Name = isEnglish
                            ? a.NameEn
                            : a.NameAr,

                        Description = isEnglish
                            ? a.DescriptionEn
                            : a.DescriptionAr,
                        SerialNumber  = a.SerialNumber, 
                        Address = a.Address,
                        FacebookUrl = a.FacebookUrl,
                        FirstPhone = a.FirstPhone,
                        SecondPhone = a.SecondPhone,
                        LogUrl = a.LogoPath
                    })
                    .FirstOrDefaultAsync(cancellation);

                if (academy is null)
                {
                    return Result.Failure<AcademyDto>(
                        Error.NotFound(
                            "Academy.NotFound",
                            "Academy was not found."));
                }

                return Result.Success(academy);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to get academy with ID {AcademyId}.",
                    academyId);

                throw;
            }
        }

        public async Task<Result<PagedList<AcademyLockupDto>>> GetAllAcademiesAsync(
            int page = 1,
            int pageSize = 5,
            CancellationToken cancellation = default)
        {
            try
            {
                page = Math.Max(page, 1);
                pageSize = Math.Max(pageSize, 5);

                var query = _dbContext.Academies
                    .AsNoTracking()
                    .Where(a => !a.IsDeleted);

                var totalCount = await query.CountAsync(cancellation);

                var isArabic =
                    CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ar";

                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new AcademyLockupDto
                    {
                        Id = a.Id,
                        SerialNumber = a.SerialNumber,

                        Name = isArabic
                            ? a.NameAr
                            : a.NameEn,

                        FirstPhone = a.FirstPhone
                    })
                    .ToListAsync(cancellation);

                var result = new PagedList<AcademyLockupDto>(
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
                    "Failed to get academies. Page: {Page}, PageSize: {PageSize}.",
                    page,
                    pageSize);

                throw;
            }
        }

        public async Task<Result<AcademyResponseDto>> UpdateAcademyAsync(
            Guid id,
            UpdateAcademyRequest request,
            CancellationToken cancellation)
        {
            try
            {
                // Normalize input
                request.NameEn = request.NameEn?.Trim() ?? string.Empty;
                request.NameAr = request.NameAr?.Trim() ?? string.Empty;
                request.DescriptionEn = request.DescriptionEn?.Trim();
                request.DescriptionAr = request.DescriptionAr?.Trim();

                // FluentValidation
                await _validator.ValidateAsync(request, cancellation);

                var academy = await _dbContext.Academies
                    .FirstOrDefaultAsync(
                        a => a.Id == id && !a.IsDeleted,
                        cancellation);

                if (academy is null)
                {
                    return Result.Failure<AcademyResponseDto>(
                        Error.NotFound(
                            "Academy.NotFound",
                            "Academy was not found."));
                }

                // Business rule: name must be unique
                var isNameExist = await _dbContext.Academies.AnyAsync(
                    a => a.Id != id &&
                         !a.IsDeleted &&
                         (a.NameEn == request.NameEn ||
                          a.NameAr == request.NameAr),
                    cancellation);

                if (isNameExist)
                {
                    return Result.Failure<AcademyResponseDto>(
                        Error.Conflict(
                            "Academy.NameAlreadyExists",
                            "Academy name already exists."));
                }

                // Business rule: logo validation
                if (request.Logo is not null &&
                    !_imageService.IsValidImage(request.Logo))
                {
                    return Result.Failure<AcademyResponseDto>(
                        Error.Validation(
                            "Academy.InvalidLogo",
                            "The provided logo is invalid."));
                }

                // Map request -> existing entity
                academy.UpdateFromRequest(request);

                if (request.Logo is not null)
                {
                    if (!string.IsNullOrWhiteSpace(academy.LogoPath))
                    {
                        _imageService.DeleteImage(academy.LogoPath);
                    }

                    academy.LogoPath = await _imageService.SaveImageAsync(
                        request.Logo,
                        "Logos");
                }

                await _dbContext.SaveChangesAsync(cancellation);

                return Result.Success(
                    academy.ToResponse());
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update academy with ID {AcademyId}.",
                    id);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while updating academy with ID {AcademyId}.",
                    id);

                throw;
            }
        }

        public async Task<Result> DeleteAcademyAsync(
            Guid academyId,
            CancellationToken cancellation)
        {
            try
            {
                var academy = await _dbContext.Academies
                    .FirstOrDefaultAsync(
                        a => a.Id == academyId && !a.IsDeleted,
                        cancellation);

                if (academy is null)
                {
                    return Result.Failure(
                        Error.NotFound(
                            "Academy.NotFound",
                            "Academy was not found."));
                }

                // Soft delete
                academy.IsDeleted = true;

                await _dbContext.SaveChangesAsync(cancellation);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete academy with ID {AcademyId}.",
                    academyId);

                throw;
            }
        }

    }
}
