# ERP.Domain / Modules

Your tables go here — one folder per ERP module. **Plain data classes, nothing else.**

```
Modules/
  Inventory/
    Entities/
      Product.cs          ← properties only
    InventoryErrors.cs    ← one static class of Error values
  Sales/
  Purchasing/
  Accounting/
  HR/
```

## The rules this layer lives by

1. **No dependencies.** `ERP.Domain.csproj` references no packages and no projects.
   No EF Core, no ASP.NET. If you need one of those, you are in the wrong layer.
2. **No logic.** No methods, no validation, no calculations that hit the database.
   An entity describes what something *is*. What it *does* belongs to the service.
3. **Public setters are fine.** The service sets them.
4. **Inherit `AuditableEntity`** — you get `Id`, audit stamps, soft delete and
   `TenantId` for free, all filled in automatically on save.
5. **Error codes are translation keys.** `Error.NotFound("Inventory.Product.NotFound", …)`
   is rendered in Arabic or English by `ar.json` / `en.json` with no code involved.

## A complete entity

```csharp
public class Product : AuditableEntity
{
    public string Sku { get; set; } = string.Empty;

    // Two languages in one row - the DTO picks which one to return.
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;

    public decimal SellingPrice { get; set; }
    public string Currency { get; set; } = "EGP";
    public decimal QuantityOnHand { get; set; }
    public bool IsActive { get; set; } = true;

    // Calculated, not stored. Add builder.Ignore(p => p.NeedsReorder) in the configuration.
    public bool NeedsReorder => QuantityOnHand <= ReorderLevel;
}
```

That is the whole file. No constructor, no factory, no `Result`, no events.

## The errors file

```csharp
public static class InventoryErrors
{
    public static readonly Error ProductNotFound =
        Error.NotFound("Inventory.Product.NotFound", "The requested product was not found.");
        //             ↑ the translation key      ↑ the fallback if the key is missing

    public static readonly Error InsufficientStock =
        Error.Conflict("Inventory.Product.InsufficientStock", "Not enough stock. Available: {0}.");
}
```

Use it from the service: `Result.Failure(InventoryErrors.ProductNotFound)`
and add the matching key to **both** `ar.json` and `en.json`.

`Error.NotFound` → 404, `Error.Conflict` → 409, `Error.Validation` → 400. The API
maps the type to the status code, so the service never mentions HTTP.
