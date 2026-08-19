using ERP.Domain.Modules.Trainee.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

internal sealed partial class PlayerConfiguration
{
    public class AcademyTraineeConfiguration
        : IEntityTypeConfiguration<AcademyTrainee>
    {
        public void Configure(EntityTypeBuilder<AcademyTrainee> builder)
        {
            builder.ToTable("AcademyTrainees");

            builder.HasKey(x => new
            {
                x.AcademyId,
                x.TraineeId
            });

            builder.Property(x => x.Status)
                .IsRequired();

            builder.HasOne(x => x.Academy)
              .WithMany(x => x.Trainees)
              .HasForeignKey(x => x.AcademyId)
              .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Trainee)
                .WithMany(x => x.Academies)
                .HasForeignKey(x => x.TraineeId)
                .OnDelete(DeleteBehavior.NoAction);

        }
    }
}
