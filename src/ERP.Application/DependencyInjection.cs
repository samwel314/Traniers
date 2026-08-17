using ERP.Application.Common.Services;
using ERP.Application.Modules.Academy;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ERP.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Application layer: module services, their validators, and the
    /// two helpers every service uses (validation + permission guard).
    ///
    /// Adding a module means adding files - never editing this method.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = ApplicationAssembly.Reference;

        services.AddScoped<IInputValidator, InputValidator>();
        services.AddScoped<IPermissionGuard, PermissionGuard>();
        services.AddScoped<IAcademyService, AcademyService>();
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddModuleServices(assembly);

        return services;
    }

    /// <summary>
    /// Every <c>XService : IXService</c> under Modules is registered automatically.
    /// One line per module is one line too many when a convention will do.
    /// </summary>
    private static IServiceCollection AddModuleServices(this IServiceCollection services, Assembly assembly)
    {
        var implementations = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .Where(t => t.Name.EndsWith("Service", StringComparison.Ordinal))
            .Where(t => t.Namespace?.Contains(".Modules.", StringComparison.Ordinal) == true);

        foreach (var implementation in implementations)
        {
            var contract = implementation.GetInterfaces()
                .FirstOrDefault(i => i.Name == $"I{implementation.Name}");

            if (contract is not null)
                services.AddScoped(contract, implementation);
        }

        return services;
    }
}

/// <summary>Assembly marker - avoids reflection on a random type name.</summary>
public static class ApplicationAssembly
{
    public static readonly Assembly Reference = typeof(ApplicationAssembly).Assembly;
}
