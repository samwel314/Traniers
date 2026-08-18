using ERP.Application.Common.Abstractions.Identity;
using ERP.Application.Common.Abstractions.Persistence;
using ERP.Application.Common.Abstractions.Services;
using ERP.Application.Modules.Academy.AcademyOutput;
using ERP.Domain.Common.Results;
using ERP.Domain.Modules.Academy.Entities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ERP.Application.Modules.AcademyAdmin
{
    
    public sealed class AcademyAdministratorService(
        IApplicationDbContext dbContext, IIdentityService identity  ,
        ICurrentUser currentUser) : IAcademyAdministratorService
    {
        public async Task<Result> AssignAsync(
            Guid academyId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var academyExists = await dbContext.Academies
                .AnyAsync(a => a.Id == academyId && !a.IsDeleted, cancellationToken);

            if (!academyExists)
            {
                return Result.Failure(
                    Error.NotFound(
                        "Academy.NotFound",
                        "Academy was not found."));
            }

            var userExists = await identity.AnyUsersByIdAsync(userId, cancellationToken); 

            if (!userExists)
            {
                return Result.Failure(
                    Error.NotFound(
                        "Identity.UserNotFound",
                        "User was not found."));
            }

            var alreadyAssigned = await dbContext.AcademyAdministrators
                .AnyAsync(
                    x => x.AcademyId == academyId &&
                         x.AdministratorId == userId,
                    cancellationToken);

            if (alreadyAssigned)
            {
                return Result.Failure(
                    Error.Conflict(
                        "Academy.AdministratorAlreadyAssigned",
                        "The administrator is already assigned to this academy."));
            }

            dbContext.AcademyAdministrators.Add(
                new AcademyAdministrator(
                    academyId,
                    userId));

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result> RemoveAsync(
            Guid academyId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var assignment = await dbContext.AcademyAdministrators
                .FirstOrDefaultAsync(
                    x => x.AcademyId == academyId &&
                         x.AdministratorId == userId,
                    cancellationToken);

            if (assignment is null)
            {
                return Result.Failure(
                    Error.NotFound(
                        "Academy.AdministratorAssignmentNotFound",
                        "The administrator is not assigned to this academy."));
            }

            dbContext.AcademyAdministrators.Remove(assignment);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result<IReadOnlyList<AcademyLockupDto>>> GetMyAcademiesAsync(
            CancellationToken cancellationToken = default)
        {
            if (currentUser.UserId is null)
            {
                return Result.Failure<IReadOnlyList<AcademyLockupDto>>(
                    Error.Unauthorized(
                        "Identity.Unauthorized",
                        "Authentication is required."));
            }

            var userId = currentUser.UserId.Value;

            var academies = await dbContext.AcademyAdministrators
                .Where(x => x.AdministratorId == userId).Select(
                   x => new AcademyLockupDto
                    {
                        Id = x.AcademyId,
                        SerialNumber = x.Academy.SerialNumber,
                        Name =CultureInfo.CurrentCulture.Name == "en" ? x.Academy.NameEn : x.Academy.NameAr,
                        FirstPhone = x.Academy.FirstPhone,
                        Location = x.Academy.Address
                    })
                .ToListAsync(cancellationToken);

            return Result.Success<IReadOnlyList<AcademyLockupDto>>(academies);
        }
    }
}
