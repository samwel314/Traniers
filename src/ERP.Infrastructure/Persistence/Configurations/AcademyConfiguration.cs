using ERP.Domain.Modules.Academy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

internal sealed partial class PlayerConfiguration
{
    public class AcademyConfiguration : IEntityTypeConfiguration<Academy>
    {
        public void Configure(EntityTypeBuilder<Academy> builder)
        {
            builder.Property(a => a.NameEn)
                .IsRequired()
                .HasMaxLength(100);
            builder.Property(a => a.NameAr)
                .IsRequired()
                .HasMaxLength(100);
            builder.Property(a => a.DescriptionEn)
                .HasMaxLength(1000);
            builder.Property(a => a.DescriptionAr)
                .HasMaxLength(1000);
            builder.Property(a => a.LogoPath)
                .HasMaxLength(500);
            builder.Property(a => a.Address)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(a => a.FirstPhone)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(a => a.SecondPhone)
                .HasMaxLength(20);

            builder.Property(a => a.Email)
                .HasMaxLength(256);

            builder.Property(a => a.WebsiteUrl)
                .HasMaxLength(500);

            builder.Property(a => a.FacebookUrl)
                .HasMaxLength(500);

            builder.Property(a => a.InstagramUrl)
                .HasMaxLength(500);

            builder.HasIndex(a => new { a.NameEn }).IsUnique();
            builder.HasIndex(a => new { a.NameAr }).IsUnique();
            builder.Property(a => a.SerialNumber)
                .UseIdentityColumn(seed: 1000, increment: 1);
        }
    }
}
