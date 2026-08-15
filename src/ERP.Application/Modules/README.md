# ERP.Application / Modules

One folder per module, and **one service interface** per module — that is the only
thing the API is allowed to know about it.

```
Modules/
  Inventory/
    Products/
      IProductService.cs          ← the use cases you can call
      ProductService.cs           ← the rules + the database calls (internal, sealed)
      IProductRepository.cs       ← what it needs from the database
      Contracts/
        ProductRequests.cs        ← input records + their validators
      Dtos/
        ProductDto.cs             ← what goes back to the client
```

Nothing here knows about EF Core, HTTP, or where translations are stored.

## The shape of a service method

Every write method reads top to bottom, in this order:

```csharp
public async Task<Result<Guid>> CreateAsync(CreateProductRequest request, CancellationToken ct)
{
    guard.Require(Permissions.Inventory.CreateProduct);   // 1. مسموح؟
    await validator.ValidateAsync(request, ct);           // 2. المدخلات شكلها صح؟

    // 3. قواعد البيزنس - if عادية
    if (await products.SkuExistsAsync(request.Sku, ct))
        return Result.Failure<Guid>(InventoryErrors.SkuAlreadyExists);

    if (request.SellingPrice < request.CostPrice)
        return Result.Failure<Guid>(InventoryErrors.PriceBelowCost);

    // 4. اعمل الصف واحفظ
    var product = new Product { Sku = request.Sku.Trim().ToUpperInvariant(), ... };

    products.Add(product);
    await unitOfWork.SaveChangesAsync(ct);                // 👈 هنا بس بيتكتب في الداتابيز
    return Result.Success(product.Id);
}
```

Reads are the same, minus steps 2 and 4.

## The two rules that matter

**1. Every business rule lives here.** Not in the entity, not in the controller.
If you want to know why stock cannot go negative, you open `ProductService.cs`
and read it top to bottom. There is no indirection to chase.

**2. Failures are values, not exceptions.** `return Result.Failure(...)` — and
because it returns before `SaveChangesAsync`, nothing was written to the database.

## Registration

You write no DI lines. `AddApplication()` registers by convention:

| Convention | Result |
|---|---|
| `XService` implementing `IXService` under `Modules/` | registered scoped |
| any `AbstractValidator<T>` | registered, found by `IInputValidator` |
| `IXRepository` → `XRepository` in Infrastructure | registered scoped |

## Multi-step operations

When several saves must succeed or fail together:

```csharp
await unitOfWork.ExecuteInTransactionAsync(async ct =>
{
    // post the invoice, move the stock, write the journal entry
    await unitOfWork.SaveChangesAsync(ct);
    return Result.Success();
}, cancellationToken);
```

## What you never do here

- Reference `Microsoft.EntityFrameworkCore` (the csproj has no such package).
- Return an entity — return a DTO.
- Hard-code a user-facing message — use a translation key from `InventoryErrors`.
