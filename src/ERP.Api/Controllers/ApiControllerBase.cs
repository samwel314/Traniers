using ERP.Application.Common.Abstractions.Services;
using ERP.Domain.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers;

/// <summary>
/// Base for every controller. It gives you one thing: a single way to turn a
/// <see cref="Result"/> into an HTTP response.
///
/// A controller action should be two lines - call the service, map the result.
/// All the thinking happens in the Application layer's services.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private ITranslator? _translator;

    protected ITranslator Translator => _translator ??= HttpContext.RequestServices.GetRequiredService<ITranslator>();

    /// <summary>Result -> 200/204, or the matching RFC 9457 problem response.</summary>
    protected IActionResult ToActionResult(Result result)
        => result.IsSuccess ? NoContent() : Problem(result.Error);

    protected IActionResult ToActionResult<T>(Result<T> result)
        => result.IsSuccess ? Ok(result.Value) : Problem(result.Error);

    /// <summary>For POSTs: 201 with a Location header pointing at the new resource.</summary>
    protected IActionResult ToCreatedResult<T>(Result<T> result, string actionName, Func<T, object> routeValues)
        => result.IsSuccess
            ? CreatedAtAction(actionName, routeValues(result.Value), result.Value)
            : Problem(result.Error);

    /// <summary>
    /// For POSTs with no GET endpoint to point at yet: 201 with the created value
    /// and no Location header. Switch to the overload above once you add a GetById.
    /// </summary>
    protected IActionResult ToCreatedResult<T>(Result<T> result)
        => result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : Problem(result.Error);

    /// <summary>
    /// The error code doubles as the resource key, so a handler that returns
    /// <c>Error.NotFound("Inventory.Product.NotFound", ...)</c> produces an Arabic
    /// message for an Arabic client with no extra work anywhere.
    /// </summary>
    private IActionResult Problem(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var localized = Translator.Get(error.Code, error.Args);

        // A key with no translation yet falls back to the English description
        // written in the domain, rather than leaking the raw key to the user.
        var detail = string.Equals(localized, error.Code, StringComparison.Ordinal)
            ? error.Description
            : localized;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Type.ToString(),
            Detail = detail,
            Instance = $"{Request.Method} {Request.Path}",
            Extensions =
            {
                ["errorCode"] = error.Code,
                ["correlationId"] = HttpContext.Items["X-Correlation-Id"]
            }
        };

        return StatusCode(status, problem);
    }
}
