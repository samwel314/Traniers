using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using ERP.Api.Middleware;
using ERP.Api.OpenApi;
using ERP.Infrastructure.Identity;
using ERP.Infrastructure.Localization;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace ERP.Api.Extensions;

public static class PresentationServices
{
    /// <summary>
    /// Fallbacks only. The real list comes from the "Localization" configuration
    /// section, which is the same section the JSON translation store reads - one
    /// source of truth for "which languages does this ERP speak".
    /// </summary>
    public static string[] SupportedCultures { get; private set; } = ["ar", "en"];

    public static string DefaultCulture { get; private set; } = "ar";

    public const string CorsPolicyName = "ErpCors";

    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllersWithJson();
        services.AddErpLocalization(configuration);
        services.AddErpCors(configuration);
        services.AddErpSwagger();
        services.AddErpRateLimiting();
        services.AddErpProblemDetails();
        services.AddErpHealthChecks(configuration);

        return services;
    }

    private static IServiceCollection AddControllersWithJson(this IServiceCollection services)
    {
        services.AddControllers(options =>
            {
                options.SuppressAsyncSuffixInActionNames = true;
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // IInputValidator owns validation; the MVC filter would produce a second,
        // non-localized 400 before the service ever runs.
        services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

        return services;
    }

    /// <summary>
    /// Culture resolution order: ?culture=ar -> Accept-Language -> the user's
    /// preferred culture claim from the JWT -> the default. Arabic requests also
    /// get an RTL hint header for the front-end.
    /// </summary>
    private static IServiceCollection AddErpLocalization(this IServiceCollection services, IConfiguration configuration)
    {
        var localization = configuration.GetSection(ErpLocalizationOptions.SectionName).Get<ErpLocalizationOptions>()
                           ?? new ErpLocalizationOptions();

        if (localization.SupportedCultures.Length > 0)
            SupportedCultures = localization.SupportedCultures;

        if (!string.IsNullOrWhiteSpace(localization.DefaultCulture))
            DefaultCulture = localization.DefaultCulture;

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var cultures = SupportedCultures.Select(c => new CultureInfo(c)).ToList();

            options.DefaultRequestCulture = new RequestCulture(DefaultCulture);
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
            options.ApplyCurrentCultureToResponseHeaders = true;

            options.RequestCultureProviders =
            [
                new QueryStringRequestCultureProvider { QueryStringKey = "culture", UIQueryStringKey = "culture" },
                new AcceptLanguageHeaderRequestCultureProvider(),
                new ClaimsRequestCultureProvider()
            ];
        });

        return services;
    }

    /// <summary>
    /// CORS is read from configuration, never hard-coded. An empty origin list in
    /// Production is treated as a configuration error rather than silently allowing
    /// everything - the usual way a CORS hole gets shipped.
    /// </summary>
    private static IServiceCollection AddErpCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                if (origins.Length == 0)
                {
                    // Development convenience only; Program.cs refuses to boot
                    // Production with an empty list.
                    policy.SetIsOriginAllowed(_ => true);
                }
                else
                {
                    policy.WithOrigins(origins).SetIsOriginAllowedToAllowWildcardSubdomains();
                }

                policy.AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()
                      // The browser can only read headers we expose explicitly.
                      .WithExposedHeaders("X-Correlation-Id", "X-Pagination", "Content-Language")
                      .SetPreflightMaxAge(TimeSpan.FromHours(1));
            });
        });

        return services;
    }

    private static IServiceCollection AddErpSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ERP API",
                Version = "v1",
                Description = "ERP backend - Clean Architecture skeleton."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the JWT here. The 'Bearer ' prefix is added for you."
            });
            options.AddSecurityDefinition("AcceptLanguage", new OpenApiSecurityScheme
            {
                Name = "Accept-Language",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Response language. Example: ar or en."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "AcceptLanguage"
            }
        },
        Array.Empty<string>()
    }
});
            // Lets a tester switch the response language straight from Swagger UI.
            //      options.OperationFilter<AcceptLanguageHeaderOperationFilter>();

            var xmlFile = $"{typeof(PresentationServices).Assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
        });

        return services;
    }

    /// <summary>
    /// A slow ERP report should not be able to take the server down, and a login
    /// endpoint should not accept unlimited password guesses.
    /// </summary>
    private static IServiceCollection AddErpRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name
                                  ?? context.Connection.RemoteIpAddress?.ToString()
                                  ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            options.AddPolicy("auth", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    private static IServiceCollection AddErpProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
            };
        });

        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }

    private static IServiceCollection AddErpHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database", tags: ["ready"]);

        return services;
    }
}

/// <summary>
/// Falls back to the culture stored on the user's profile (carried in the JWT)
/// when the client sends no preference of its own.
/// </summary>
public sealed class ClaimsRequestCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var culture = httpContext.User.FindFirst(ErpClaims.Culture)?.Value;

        return Task.FromResult(string.IsNullOrWhiteSpace(culture)
            ? null
            : new ProviderCultureResult(culture, culture));
    }
}
