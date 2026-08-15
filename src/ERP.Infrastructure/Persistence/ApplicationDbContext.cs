using ERP.Application.Common.Abstractions.Services;
using ERP.Domain.Common;
using ERP.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence;

/// <summary>
/// The single write model of the system.
///
/// Notice there are no <c>DbSet</c> properties: entity types are discovered from
/// the <c>IEntityTypeConfiguration</c> classes in this assembly, so adding a module
/// never means editing this file. Repositories reach entities through <c>Set&lt;T&gt;()</c>.
/// </summary>
public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ICurrentUser currentUser)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    /// <summary>
    /// Captured once per context instance. Used by the tenant query filter below;
    /// EF turns it into a query parameter rather than baking it into the SQL.
    /// </summary>
    private Guid CurrentTenantId => currentUser.TenantId ?? Guid.Empty;

    /// <summary>Set to true by maintenance jobs / seeders that must see every tenant.</summary>
    public bool IgnoreTenantFilter { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Every IEntityTypeConfiguration<> in this assembly, no manual list to maintain.
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.ApplyIdentityTableNames();
        builder.ApplyDecimalPrecision();
        builder.ApplyUtcDateTimeConversion();

        // Soft-deleted rows and other tenants' rows simply do not exist for queries.
        builder.ApplyGlobalFilters<ISoftDeletable>(e => !e.IsDeleted);
        builder.ApplyGlobalFilters<IHasTenant>(e => IgnoreTenantFilter || e.TenantId == CurrentTenantId);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configuration)
    {
        base.ConfigureConventions(configuration);

        configuration.Properties<string>().HaveMaxLength(256);
        configuration.Properties<decimal>().HavePrecision(18, 4);
    }
}
