using ERP.Domain.Modules.Academy.Entities;
using ERP.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

internal sealed partial class PlayerConfiguration
{
    public sealed class AcademyAdministratorConfiguration
        : IEntityTypeConfiguration<AcademyAdministrator>
    {
        public void Configure(EntityTypeBuilder<AcademyAdministrator> builder)
        {
            builder.ToTable("AcademyAdministrators");

            builder.HasKey(x => new
            {
                x.AcademyId,
                x.AdministratorId
            });

            builder.HasOne(x => x.Academy)
                .WithMany(x => x.Administrators)
                .HasForeignKey(x => x.AcademyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.AdministratorId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
