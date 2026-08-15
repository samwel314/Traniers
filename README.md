# ERP — Clean Architecture Skeleton

A .NET 10 ERP backend skeleton. Every cross-cutting concern is wired and working;
**the business logic is intentionally empty** — that part is yours.

---

## The dependency rule

Dependencies point inwards only. Nothing in an inner circle knows anything about
an outer one.

```
            ┌───────────────────────────────────────────┐
            │                ERP.Api                    │  controllers, middleware,
            │        (composition root, HTTP)           │  Swagger, CORS, Serilog
            └───────────────────┬───────────────────────┘
                                │
            ┌───────────────────▼───────────────────────┐
            │            ERP.Infrastructure             │  EF Core, Identity, JWT,
            │     (implements what Application needs)   │  JSON i18n, cache, clock
            └───────────────────┬───────────────────────┘
                                │
            ┌───────────────────▼───────────────────────┐
            │             ERP.Application               │  services (use cases),
            │      (use cases + abstractions only)      │  validation, DTOs
            └───────────────────┬───────────────────────┘
                                │
            ┌───────────────────▼───────────────────────┐
            │               ERP.Domain                  │  plain entities,
            │        (zero dependencies at all)         │  Result, errors
            └───────────────────────────────────────────┘
```

| Project | References | Never references |
|---|---|---|
| `ERP.Domain` | *nothing* | any package at all |
| `ERP.Application` | Domain, FluentValidation | EF Core, ASP.NET, Infrastructure |
| `ERP.Infrastructure` | Application, EF Core, Identity | Api |
| `ERP.Api` | Application (+ Infrastructure for one DI call) | — |

`ERP.Api` references `ERP.Infrastructure` for exactly one line — `AddInfrastructure()`
in `Program.cs`. No controller may touch it.

---

## What is already handled

### Logging — Serilog
Configured entirely from `appsettings.json`. Console + daily rolling files (30 days).
Every request carries a **correlation id** (`X-Correlation-Id`, reused if the caller
sends one), pushed into the log context, echoed in the response header and attached
to every error body. Serilog request logging records every request with its status,
duration, user and culture, and warns on anything over one second; health probes are
logged at Verbose so they do not drown the file.

### Localization — Arabic + English, two layers deep
1. **Messages** — plain JSON files, one per culture:
   `src/ERP.Infrastructure/Localization/Resources/ar.json` and `en.json`. They are
   reached only through the `ITranslator` abstraction, so no use case knows where
   the text lives. Validation messages, API errors and Identity errors are all
   localized. Error codes double as translation keys, so
   `Error.NotFound("Inventory.Product.NotFound", …)` renders in the caller's language
   with no extra work.

   The files are **copied next to the binaries, not embedded**, and reloaded when
   they change on disk — a wrong wording can be fixed in a deployed environment
   without a rebuild. Keys may be nested or dotted; both resolve the same:

   ```json
   { "Validation": { "Required": "هذا الحقل مطلوب." } }   // == "Validation.Required"
   ```

   Culture fallback is `ar-EG` → `ar` → default. A key missing everywhere is
   returned as-is and logged, never thrown.
2. **Data** — an entity keeps both spellings in one row (`NameAr` / `NameEn` columns),
   and the DTO picks the one matching the request's language.

Culture resolution order: `?culture=ar` → `Accept-Language` → the user's preferred
culture claim from the JWT → default (`ar`). Swagger UI gets a language dropdown.

### CORS
Read from `Cors:AllowedOrigins` — never hard-coded. Wildcard subdomains, credentials,
1-hour preflight cache, and `X-Correlation-Id` / `X-Pagination` / `Content-Language`
exposed so the browser can actually read them. **Production refuses to boot** with an
empty origin list rather than silently allowing everything.

### Error handling
One `IExceptionHandler` for the whole app; no controller has a `try/catch`. Maps to
RFC 9457 `ProblemDetails`: validation → 400 with per-field errors, forbidden → 403,
domain invariant → 422, concurrency → 409, everything else → 500 with a generic
localized message. Stack traces appear in Development only.

### Persistence
EF Core + SQL Server, with **automatic**:
- audit stamps (`CreatedBy`/`CreatedAtUtc`/`ModifiedBy`/`ModifiedAtUtc`),
- soft delete — a `DELETE` is rewritten as a flag, and deleted rows disappear from every query,
- multi-tenancy — a global filter per `TenantId`, so a query cannot leak another company's data,
- UTC conversion for every `DateTime`, and `decimal(18,4)` everywhere,
- one place to look for every rule: the service.

### Security
JWT bearer + ASP.NET Identity (sealed inside Infrastructure behind `IIdentityService`),
refresh tokens, lockout after 5 failed attempts, permission claims flattened into the
token so authorization needs no database round trip, and `IPermissionGuard.Require(...)`
enforced inside the service rather than on the controller — so a background job or a
message consumer calling the same method is covered by the same rule.

### Also wired
API versioning (URL segment + header), Swagger with JWT auth and per-version documents,
rate limiting (global + a stricter `auth` policy for login), `/health/live` and
`/health/ready`, response caching abstraction, and startup guards that refuse to run
Production with a placeholder signing key.

---

## Where your code goes

| You want to add | Put it in |
|---|---|
| An entity (table) | `src/ERP.Domain/Modules/<Module>/Entities/` |
| A business rule | an `if` in the service, returning `Result.Failure(...)` |
| A use case | a method on `IXService` in `src/ERP.Application/Modules/<Module>/` |
| Input rules | a `LocalizedValidator<T>` in that module's `Contracts/` |
| A table mapping | `src/ERP.Infrastructure/Persistence/Configurations/` |
| A custom repository | `src/ERP.Infrastructure/Persistence/Repositories/` (auto-registered) |
| An endpoint | `src/ERP.Api/Controllers/` |
| A message | both `Localization/Resources/ar.json` **and** `en.json` |

Each of those folders has a `README.md` with the exact shape to follow.

### The sample module

`Inventory / Products` is a complete working example across all four layers — read it
as the reference, then delete it. It is 7 files plus a controller:

```
Domain/Modules/Inventory/          Product entity + InventoryErrors
Application/Modules/Inventory/     IProductService + ProductService, IProductRepository, contracts, DTOs
Infrastructure/Persistence/        ProductConfiguration, ProductRepository
Api/Controllers/                   ProductsController
```

To remove it: delete those four folders, the `Inventory` block in `Permissions.cs`,
the `Inventory` block in `ar.json` / `en.json`, and drop the migration.

It demonstrates, end to end: a business rule returning `Result` (stock refusing to
go negative, with the available quantity carried into the localized message), a
uniqueness check, cache invalidation, per-tenant isolation, and audit stamps written
without a single line in the service.

**You should never have to edit `DependencyInjection.cs`** — services, validators,
entity configurations and repositories are all discovered by assembly scanning.

---

## Running it

```bash
# 1. Point it at your database
dotnet user-secrets set "ConnectionStrings:Default" "Server=...;Database=ErpDb;..." --project src/ERP.Api
dotnet user-secrets set "Jwt:Key" "a-real-32-character-minimum-secret" --project src/ERP.Api

# 2. Create the first migration (once you have your first entity)
dotnet tool install --global dotnet-ef
dotnet ef migrations add Initial -p src/ERP.Infrastructure -s src/ERP.Api

# 3. Run - Development migrates and seeds automatically
dotnet run --project src/ERP.Api
```

Swagger: `https://localhost:<port>/swagger`

The administrator account is seeded only if `Seed:Administrator:Password` is set —
there is no built-in default password.

---

## Notes on package choices

- **No MediatR, no CQRS, no DDD.** Plain service layer: the controller calls a
  service, the service holds the rules and talks to the repository. Entities are
  plain data classes. Nothing is dispatched, mediated, or resolved by reflection
  at runtime - you can read any request top to bottom in one file.
- **Swashbuckle is pinned to 9.0.6** — version 10 moved to Microsoft.OpenApi 2.x,
  which renames the namespaces every `OperationFilter` uses.
- Versions are centralized in `Directory.Packages.props`; individual `.csproj` files
  carry no version numbers.
