using ERP.Domain.Modules.Academy.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Common.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<Academy> Academies { get; }
    DbSet<AcademyAdministrator> AcademyAdministrators {   get;}
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}