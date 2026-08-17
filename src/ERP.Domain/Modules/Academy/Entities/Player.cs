using ERP.Domain.Common;

namespace ERP.Domain.Modules.Academy.Entities;

/// <summary>
/// A player enrolled in the academy. Plain data - one property per column.
///
/// All the rules (is the age allowed? is the code already taken?) live in
/// PlayerService. This class only describes what a player *is*.
/// </summary>
public class Player : AuditableEntity
{
    /// <summary>Academy code, e.g. "PL-0001". Unique per academy.</summary>
    public string Code { get; set; } = string.Empty;

    public string FullNameAr { get; set; } = string.Empty;
    public string FullNameEn { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string? NationalId { get; set; }

    public Sport Sport { get; set; }
    public PlayerLevel Level { get; set; }

    // Most players are minors, so the guardian is who the academy actually calls.
    public string GuardianName { get; set; } = string.Empty;
    public string GuardianPhone { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public DateOnly EnrollmentDate { get; set; }
    public decimal MonthlyFee { get; set; }
    public string Currency { get; set; } = "EGP";

    public bool IsActive { get; set; } = true;
}
