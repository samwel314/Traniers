using System.ComponentModel.DataAnnotations;

namespace ERP.Infrastructure.Identity;

/// <summary>
/// Bound from configuration section "Jwt" and validated at startup, so a missing
/// signing key fails the boot instead of the first login attempt.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required, MinLength(32, ErrorMessage = "Jwt:Key must be at least 32 characters.")]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = "ERP.Api";

    [Required]
    public string Audience { get; set; } = "ERP.Client";

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 60;

    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 14;
}

/// <summary>Claim names used across the app - never spelled out inline.</summary>
public static class ErpClaims
{
    public const string TenantId = "tenant_id";
    public const string Permission = "permission";
    public const string Culture = "culture";
}
