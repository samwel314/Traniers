using ERP.Infrastructure.Services;
using Serilog.Context;

namespace ERP.Api.Middleware;

/// <summary>
/// Gives every request an id: reused if the caller sent one (so a correlation id
/// survives across microservices), generated otherwise. It is pushed into the
/// Serilog context, echoed in a response header, and attached to error responses -
/// one id ties a user's complaint to every log line it produced.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(RequestContext.HeaderName, out var supplied)
                            && !string.IsNullOrWhiteSpace(supplied)
            ? supplied.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[RequestContext.HeaderName] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[RequestContext.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
