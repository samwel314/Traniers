using Microsoft.AspNetCore.Identity;

namespace ERP.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity user. It lives in Infrastructure on purpose - the
/// Application layer only knows <c>IIdentityService</c> and <c>ICurrentUser</c>,
/// so replacing Identity with Keycloak/Auth0 touches this project alone.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string? FullNameAr { get; set; }
    public string? FullNameEn { get; set; }

    /// <summary>Which company/branch this user belongs to. Flows into every query filter.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Preferred UI culture ("ar" / "en"), used when no Accept-Language is sent.</summary>
    public string PreferredCulture { get; set; } = "ar";

    public bool IsActive { get; set; } = true;

    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAtUtc { get; set; }
}

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }

    public ApplicationRole(string name) : base(name) => NormalizedName = name.ToUpperInvariant();

    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
}
