using ERP.Domain.Common.Results;

namespace ERP.Domain.Modules.Academy;

/// <summary>
/// Every failure this module can produce, in one place.
/// The Code is the translation key in ar.json / en.json - which is why a service
/// can return a failure without knowing what language the caller reads.
/// </summary>
public static class AcademyErrors
{
    public static readonly Error PlayerNotFound =
        Error.NotFound("Academy.Player.NotFound", "The requested player was not found.");

    public static readonly Error CodeRequired =
        Error.Validation("Academy.Player.CodeRequired", "Player code is required.");

    public static readonly Error CodeAlreadyExists =
        Error.Conflict("Academy.Player.CodeAlreadyExists", "A player with the same code already exists.");

    public static readonly Error NationalIdAlreadyExists =
        Error.Conflict("Academy.Player.NationalIdAlreadyExists", "A player with the same national ID is already enrolled.");

    public static readonly Error NameRequired =
        Error.Validation("Academy.Player.NameRequired", "Player name is required.");

    /// <summary>Args: {0} = minimum age, {1} = maximum age.</summary>
    public static readonly Error AgeOutOfRange =
        Error.Validation("Academy.Player.AgeOutOfRange", "Player age must be between {0} and {1} years.");

    public static readonly Error BirthDateInFuture =
        Error.Validation("Academy.Player.BirthDateInFuture", "Date of birth cannot be in the future.");

    public static readonly Error GuardianRequired =
        Error.Validation("Academy.Player.GuardianRequired", "A guardian name and phone are required for players under 18.");

    public static readonly Error FeeNegative =
        Error.Validation("Academy.Player.FeeNegative", "Monthly fee cannot be negative.");

    public static readonly Error EnrollmentDateInFuture =
        Error.Validation("Academy.Player.EnrollmentDateInFuture", "Enrollment date cannot be in the future.");
}
