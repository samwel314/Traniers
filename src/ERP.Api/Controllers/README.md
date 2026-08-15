# ERP.Api / Controllers

Controllers are thin. Call the service, map the `Result`, return. No business rules,
no `try/catch`, no `DbContext`, no `SaveChanges`, no permission checks.

```csharp
[ApiVersion("1.0")]
[Authorize]
public sealed class ProductsController(IProductService products) : ApiControllerBase
{
    /// <summary>Creates a product.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct(CreateProductRequest request, CancellationToken ct)
        => ToCreatedResult(await products.CreateAsync(request, ct), nameof(GetProduct), id => new { id });

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken ct)
        => ToActionResult(await products.GetByIdAsync(id, ct));
}
```

A controller depends on **one service interface**. If it needs a repository or an
entity, the logic is in the wrong layer.

## What you get for free

| Concern | Where it is handled |
|---|---|
| Route + versioning | `ApiControllerBase` — `api/v{version}/[controller]` |
| Authentication | `[Authorize]` on the controller |
| Permissions | `guard.Require(...)` inside the service — so jobs and consumers are covered too |
| Validation | `IInputValidator` in the service → localized 400 `ValidationProblemDetails` |
| Errors | `GlobalExceptionHandler` → localized RFC 9457 `ProblemDetails` |
| `Result` → HTTP status | `ToActionResult` / `ToCreatedResult` |
| Language | `UseRequestLocalization` — `?culture=ar`, `Accept-Language`, or the JWT claim |
| Transaction | the service calls `SaveChangesAsync` / `ExecuteInTransactionAsync` |
| Logging | Serilog request logging + correlation id, automatic |
| Rate limiting | global; add `[EnableRateLimiting("auth")]` on login endpoints |

## Conventions

- One controller per module service, named in the plural: `ProductsController`.
- Always take a `CancellationToken` and pass it down.
- Declare `[ProducesResponseType]` for every status you can return — it is what
  makes the generated Swagger worth reading.
- Never return a domain entity. Return the DTO the service produced.
