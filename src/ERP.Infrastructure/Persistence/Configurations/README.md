# Persistence / Configurations

One `IEntityTypeConfiguration<T>` per aggregate. They are discovered automatically
by `ApplyConfigurationsFromAssembly` in `ApplicationDbContext`, which is why that
class has no `DbSet` properties and never needs editing.

```csharp
internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", "inventory");
        builder.HasKey(p => p.Id);

        // Value objects map to plain columns, not JSON - reports still need to filter on them.
        builder.OwnsOne(p => p.Name, name => name.ConfigureLocalizedText("Name"));

        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(m => m.Amount).HasColumnName("Price").HasPrecision(18, 4);
            price.Property(m => m.Currency).HasColumnName("PriceCurrency").HasMaxLength(3);
        });

        builder.OwnsOne(p => p.Sku, sku =>
            sku.Property(s => s.Value).HasColumnName("Sku").HasMaxLength(32).IsRequired());

        // Uniqueness is per tenant, and soft-deleted rows must not block a new SKU.
        builder.HasIndex("TenantId", "Sku")
               .IsUnique()
               .HasFilter("[IsDeleted] = 0");

        builder.HasOne(p => p.Category)
               .WithMany()
               .HasForeignKey(p => p.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);   // never cascade in an ERP

        // Domain events are behaviour, not data.
        builder.Ignore(p => p.DomainEvents);
    }
}
```

## Applied to every entity automatically

Set once in `ModelBuilderExtensions`, so you do not repeat them here:

- `decimal` → `(18,4)`
- `DateTime` → stored and read back as UTC
- `string` → `nvarchar(256)` unless you say otherwise
- soft-delete filter on `ISoftDeletable`
- tenant filter on `IHasTenant`
- Identity tables renamed into the `security` schema

## Migrations

```bash
dotnet ef migrations add <Name> -p src/ERP.Infrastructure -s src/ERP.Api
dotnet ef database update            -p src/ERP.Infrastructure -s src/ERP.Api
```
