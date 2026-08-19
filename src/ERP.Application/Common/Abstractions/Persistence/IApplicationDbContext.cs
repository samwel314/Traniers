using ERP.Domain.Modules.Academy.Entities;
using ERP.Domain.Modules.Trainee.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Common.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<Academy> Academies { get; }
    DbSet<AcademyAdministrator> AcademyAdministrators {   get;}
    DbSet<AcademyTrainee> AcademyTrainees { get; }  
    DbSet<Trainee> Trainees { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}