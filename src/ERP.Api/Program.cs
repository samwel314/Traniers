using ERP.Api.Extensions;
using ERP.Api.Middleware;
using ERP.Application;
using ERP.Application.Common.Validation;
using ERP.Infrastructure;
using ERP.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Serilog;

// A bootstrap logger so failures *during* startup are still captured.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ---- Logging: fully configuration-driven (see appsettings.json "Serilog") ----
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", "ERP.Api")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName));

    // ---- The three layers, one call each. Order does not matter here. ----
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddPresentation(builder.Configuration);


    var app = builder.Build();

    GuardProductionConfiguration(app);

    // =====================================================================
    // Middleware order is a contract, not a preference. Top to bottom:
    // =====================================================================

    // 1. Exceptions first, so it wraps everything below it.
    app.UseStaticFiles();   
    app.UseExceptionHandler();
    app.UseStatusCodePages();

    // 2. Correlation id - every log line under here carries it.
    app.UseCorrelationId();

    // 3. Request logging, now that the correlation id exists.
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "{RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

        options.GetLevel = (httpContext, elapsed, ex) =>
            ex is not null || httpContext.Response.StatusCode >= 500
                ? Serilog.Events.LogEventLevel.Error
                : elapsed > 1000
                    ? Serilog.Events.LogEventLevel.Warning
                    // Health probes every few seconds would drown the log.
                    : httpContext.Request.Path.StartsWithSegments("/health")
                        ? Serilog.Events.LogEventLevel.Verbose
                        : Serilog.Events.LogEventLevel.Information;

        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            diagnosticContext.Set("Culture", Thread.CurrentThread.CurrentUICulture.Name);
            diagnosticContext.Set("UserId", httpContext.User.Identity?.Name);
        };
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "ERP API");
            options.DisplayRequestDuration();
        });
    }
    else
    {
        // HSTS only outside development - it would pin localhost to HTTPS in the browser.
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    // 4. Culture must be resolved before anything reads a resource string.
    app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

    // 5. CORS before auth: a rejected preflight must not need a token.
    app.UseCors(PresentationServices.CorsPolicyName);

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    // The global limiter covers every endpoint; the "auth" policy is opted into
    // per-controller with [EnableRateLimiting("auth")].
    app.MapControllers();

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false // liveness: is the process up at all
    }).AllowAnonymous();

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready") // readiness: can it serve traffic
    }).AllowAnonymous();

    await InitializeDatabaseAsync(app);

    Log.Information("ERP API started in {Environment}", app.Environment.EnvironmentName);
    await app.RunAsync();
    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "ERP API terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Startup checks for the mistakes that are cheap to make and expensive to discover
// in production: a wide-open CORS policy, or a signing key committed to appsettings.
static void GuardProductionConfiguration(WebApplication app)
{
    if (!app.Environment.IsProduction())
        return;

    var origins = app.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Length == 0)
        throw new InvalidOperationException(
            "Cors:AllowedOrigins must be configured in Production - refusing to allow every origin.");

    var key = app.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(key) || key.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException(
            "Jwt:Key must be supplied from a secret store in Production.");
}

// Migrates and seeds on boot. Convenient in development; in production this is
// normally a separate deployment step, hence the configuration switch.
static async Task InitializeDatabaseAsync(WebApplication app)
{
    if (!app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        return;

    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    
    await seeder.MigrateAsync();
    await seeder.SeedAsync();
}

/// <summary>Exposed so integration tests can spin the real host up with WebApplicationFactory.</summary>
public partial class Program;
