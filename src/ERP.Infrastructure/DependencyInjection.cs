using System.Reflection;
using System.Text;
using ERP.Application.Common.Abstractions.Identity;
using ERP.Application.Common.Abstractions.Persistence;
using ERP.Application.Common.Abstractions.Services;
using ERP.Infrastructure.Identity;
using ERP.Infrastructure.Localization;
using ERP.Infrastructure.Persistence;
using ERP.Infrastructure.Persistence.Interceptors;
using ERP.Infrastructure.Persistence.Repositories;
using ERP.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ERP.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Wires every abstraction the Application layer declared to a concrete
    /// implementation. This is the only method in the solution that knows both sides.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddIdentityAndAuth(configuration);
        services.AddLocalizationServices(configuration);
        services.AddCommonServices();

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                // Transient network faults are normal against Azure SQL; retry instead of failing the request.
                sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), null);
                sql.CommandTimeout(60);
            });

            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());

            if (configuration.GetValue<bool>("Database:EnableSensitiveDataLogging"))
            {
                // Development only: prints parameter values into the logs.
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Any entity gets a working repository with zero code.
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // ...and module-specific repositories (IProductRepository -> ProductRepository)
        // are picked up automatically by convention.
        services.AddRepositoriesByConvention(typeof(DependencyInjection).Assembly);

        services.AddScoped<DatabaseSeeder>();

        return services;
    }

    /// <summary>
    /// Registers every <c>X : IX</c> pair found under Persistence/Repositories,
    /// so adding a repository never means editing this file.
    /// </summary>
    private static IServiceCollection AddRepositoriesByConvention(this IServiceCollection services, Assembly assembly)
    {
        var implementations = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .Where(t => t.Namespace?.Contains("Persistence.Repositories", StringComparison.Ordinal) == true);

        foreach (var implementation in implementations)
        {
            var contract = implementation.GetInterfaces()
                .FirstOrDefault(i => i.Name == $"I{implementation.Name}");

            if (contract is not null)
                services.AddScoped(contract, implementation);
        }

        return services;
    }

    private static IServiceCollection AddIdentityAndAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart(); // a bad signing key fails the boot, not the first login

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.User.RequireUniqueEmail = true;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(jwt.Key)
                            ? new string('0', 32)
                            : jwt.Key)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }

    private static IServiceCollection AddLocalizationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Translations are JSON files at Localization/Resources/{culture}.json,
        // copied to the output directory and reloaded when edited on disk.
        services.AddOptions<ErpLocalizationOptions>()
            .Bind(configuration.GetSection(ErpLocalizationOptions.SectionName));

        // Singleton: it caches every parsed file and owns the file watcher.
        services.AddSingleton<JsonTranslationStore>();
        services.AddSingleton<ITranslator, Translator>();

        return services;
    }

    private static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        // Singleton, not scoped: it holds no state of its own, it reads the ambient
        // HttpContext on each call. It has to be a singleton because IExceptionHandler
        // is registered as one and consumes it.
        services.AddSingleton<IRequestContext, RequestContext>();

        return services;
    }
}
