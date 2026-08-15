using System.Globalization;
using ERP.Application.Common.Abstractions.Services;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Common.Exceptions;
using ERP.Infrastructure.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Middleware;

/// <summary>
/// The single exit point for anything that throws. No controller has a try/catch,
/// and no stack trace ever reaches a client.
///
/// Every message is resolved through <see cref="ITranslator"/>, so the same failure
/// is reported in Arabic or English according to the caller's culture.
/// </summary>
public sealed class GlobalExceptionHandler(
    ITranslator translator,
    IRequestContext requestContext,
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // The culture is read off the request, not from CultureInfo.CurrentUICulture.
        // This middleware sits *outside* UseRequestLocalization so that it catches
        // everything - which means by the time an exception unwinds back to here,
        // the ambient culture has already been restored to the process default.
        // Without this, an Arabic client would get "Access denied" in English.
        var culture = httpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture
                      ?? CultureInfo.CurrentUICulture;

        var problem = Map(exception, culture);

        problem.Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}";
        problem.Extensions["correlationId"] = requestContext.CorrelationId;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (environment.IsDevelopment())
        {
            problem.Extensions["exception"] = exception.GetType().FullName;
            problem.Extensions["stackTrace"] = exception.ToString();
        }

        if (problem.Status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception ({CorrelationId})", requestContext.CorrelationId);
        else
            logger.LogWarning("Request failed with {Status}: {Message}", problem.Status, exception.Message);

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem
        });
    }

    private const int ClientClosedRequest = 499;

    private ProblemDetails Map(Exception exception, CultureInfo culture)
    {
        string T(string key) => translator.GetFor(culture, key);

        return exception switch
        {
            ValidationException validation => new ValidationProblemDetails(validation.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = T("Validation.Title"),
                Detail = T("Validation.Detail"),
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
            },

            ForbiddenAccessException forbidden => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = T("Error.Forbidden.Title"),
                Detail = T("Error.Forbidden"),
                Extensions = { ["requiredPermission"] = forbidden.Permission }
            },

            UnauthorizedAccessException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = T("Error.Unauthorized.Title"),
                Detail = T("Error.Unauthorized")
            },

            // A broken invariant is a bug, but the message is a business one - show it.
            DomainException domain => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = T("Error.Conflict.Title"),
                Detail = T(domain.Error.Code) is var localized && localized != domain.Error.Code
                    ? localized
                    : domain.Message,
                Extensions = { ["errorCode"] = domain.Error.Code }
            },

            DbUpdateConcurrencyException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = T("Error.Conflict.Title"),
                Detail = T("Error.Concurrency")
            },

            OperationCanceledException or TaskCanceledException => new ProblemDetails
            {
                // 499: the client went away. Not an error worth alerting on.
                Status = ClientClosedRequest,
                Title = T("Error.RequestCancelled"),
                Detail = T("Error.RequestCancelled")
            },

            // Anything unrecognised: a generic message. Details stay in the logs.
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = T("Error.Unexpected.Title"),
                Detail = T("Error.Unexpected")
            }
        };
    }
}
