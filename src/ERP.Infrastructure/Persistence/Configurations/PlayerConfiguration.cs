using ERP.Domain.Modules.Academy.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the Player entity to its table. Discovered automatically by
/// ApplyConfigurationsFromAssembly - the DbContext was never edited to add it.
/// </summary>
internal sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players", "academy");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Code).HasMaxLength(32).IsRequired();

        builder.Property(p => p.FullNameAr).HasMaxLength(200).IsRequired();
        builder.Property(p => p.FullNameEn).HasMaxLength(200).IsRequired();

        builder.Property(p => p.NationalId).HasMaxLength(14);

        // Stored as text, not numbers: "Football" in the table beats "0" when
        // someone reads the data straight from SQL - and adding a sport later
        // cannot silently shift the meaning of existing rows.
        builder.Property(p => p.Sport).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(p => p.Level).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(p => p.Gender).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.Property(p => p.GuardianName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.GuardianPhone).HasMaxLength(20).IsRequired();

        builder.Property(p => p.Phone).HasMaxLength(20);
        builder.Property(p => p.Email).HasMaxLength(256);
        builder.Property(p => p.Notes).HasMaxLength(2000);

        builder.Property(p => p.MonthlyFee).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();

        builder.Property(p => p.IsActive).HasDefaultValue(true);

        builder.HasIndex(p => p.Code).HasDatabaseName("IX_Players_Code");
        builder.HasIndex(p => p.NationalId).HasDatabaseName("IX_Players_NationalId");
        builder.HasIndex(p => p.Sport).HasDatabaseName("IX_Players_Sport");

        // Tenant + soft-delete filters are applied globally in ApplicationDbContext,
        // but the index has to exist for those filters to be cheap.
        builder.HasIndex(p => new { p.TenantId, p.IsDeleted })
               .HasDatabaseName("IX_Players_Tenant_NotDeleted");

        builder.Property(p => p.CreatedBy).HasMaxLength(128);
        builder.Property(p => p.ModifiedBy).HasMaxLength(128);
        builder.Property(p => p.DeletedBy).HasMaxLength(128);
    }
}
