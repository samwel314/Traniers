using ERP.Domain.Modules.Trainee.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

internal sealed partial class PlayerConfiguration
{
    public class TraineeConfiguration : IEntityTypeConfiguration<Trainee>
    {
        public void Configure(EntityTypeBuilder<Trainee> builder)
        {
            builder.ToTable("Trainees");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.PhoneNumber)
                .HasMaxLength(20);

            builder.Property(x => x.Age)
                .IsRequired();

            builder.Property(x => x.Gender)
                .IsRequired();

            builder.Property(x => x.Photo)
                .HasMaxLength(500);

            builder.HasIndex(x => x.PhoneNumber);

            builder.HasOne(x => x.Parent)
                .WithMany(x => x.Trainees)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.NoAction);


        }
    }
}
