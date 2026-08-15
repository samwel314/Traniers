using System.Security.Claims;
using ERP.Application.Common.Security;
using ERP.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Persistence;

/// <summary>
/// Brings a fresh database to a usable state: schema, roles, permission claims and
/// one administrator. Safe to run on every boot - every step is idempotent.
/// </summary>
public sealed class DatabaseSeeder(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IConfiguration configuration,
    ILogger<DatabaseSeeder> logger)
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsRelational())
            return;

        logger.LogInformation("Applying database migrations...");
        await context.Database.MigrateAsync(cancellationToken);
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        context.IgnoreTenantFilter = true;

        await SeedRolesAsync();
        await SeedAdministratorAsync();

        context.IgnoreTenantFilter = false;
    }

    private async Task SeedRolesAsync()
    {
        foreach (var roleName in Roles.All)
        {
            var role = await roleManager.FindByNameAsync(roleName);

            if (role is null)
            {
                role = new ApplicationRole(roleName);
                await roleManager.CreateAsync(role);
                logger.LogInformation("Seeded role {Role}", roleName);
            }

            // The administrator holds every declared permission; other roles are
            // configured by the customer at runtime.
            if (roleName != Roles.Administrator)
                continue;

            var existing = (await roleManager.GetClaimsAsync(role))
                .Where(c => c.Type == ErpClaims.Permission)
                .Select(c => c.Value)
                .ToHashSet();

            foreach (var permission in Permissions.All.Where(p => !existing.Contains(p)))
                await roleManager.AddClaimAsync(role, new Claim(ErpClaims.Permission, permission));
        }
    }

    private async Task SeedAdministratorAsync()
    {
        var userName = configuration["Seed:Administrator:UserName"] ?? "admin";
        var email = configuration["Seed:Administrator:Email"] ?? "admin@erp.local";
        var password = configuration["Seed:Administrator:Password"];
        var tenantId = configuration.GetValue<Guid?>("Seed:Administrator:TenantId") ?? Guid.Empty;

        if (string.IsNullOrWhiteSpace(password))
        {
            // No password configured = no default account. Never ship a known one.
            logger.LogWarning("Seed:Administrator:Password is not set - skipping administrator seeding.");
            return;
        }

        if (await userManager.FindByNameAsync(userName) is not null)
            return;

        var admin = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            TenantId = tenantId,
            PreferredCulture = "ar",
            IsActive = true
        };

        var created = await userManager.CreateAsync(admin, password);

        if (!created.Succeeded)
        {
            logger.LogError("Failed to seed administrator: {Errors}",
                string.Join("; ", created.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, Roles.Administrator);
        logger.LogInformation("Seeded administrator {UserName}", userName);
    }
}
