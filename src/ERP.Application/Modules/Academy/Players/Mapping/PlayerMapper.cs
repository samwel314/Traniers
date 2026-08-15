using ERP.Application.Modules.Academy.Players.Contracts;
using ERP.Domain.Modules.Academy.Entities;

namespace ERP.Application.Modules.Academy.Players.Mapping;

/// <summary>
/// Manual mapping for the Players module.
///
/// Written by hand on purpose - no AutoMapper, no Mapster. The cost is a few lines
/// per property; what you get back is that a renamed property becomes a compile
/// error here instead of a silently null column at runtime, and you can F12 into
/// the mapping like any other method.
///
/// Two jobs, in this order:
///   <see cref="Normalize"/>  - clean the input (trim, casing, empty -> null)
///   <see cref="ToEntity"/>   - copy the clean input onto a new row
///
/// The rule for what belongs in this file: **shaping only**.
/// "Is the age allowed?" and "is the code taken?" are decisions, and they live in
/// PlayerService. If a method here would need a repository or the clock, it is not
/// a mapping.
/// </summary>
public static class PlayerMapper
{
    /// <summary>
    /// Returns a cleaned copy of the request.
    ///
    /// The service calls this **before** validating, which is the whole point:
    /// the validator and the entity then see exactly the same values. Otherwise a
    /// pasted "  PL-0001  " gets rejected for a trailing space that the system
    /// would have trimmed away anyway.
    /// </summary>
    public static CreatePlayerRequest Normalize(CreatePlayerRequest request) => request with
    {
        // Uppercased so "pl-0001", "PL-0001" and " PL-0001 " cannot become three
        // different players that all pass the uniqueness check.
        Code = request.Code?.Trim().ToUpperInvariant() ?? string.Empty,

        FullNameAr = request.FullNameAr?.Trim(),
        FullNameEn = request.FullNameEn?.Trim(),

        NationalId = NullIfBlank(request.NationalId),

        GuardianName = request.GuardianName?.Trim() ?? string.Empty,
        GuardianPhone = request.GuardianPhone?.Trim() ?? string.Empty,

        Phone = NullIfBlank(request.Phone),

        // Lowercased so the same address cannot be stored twice in two casings.
        Email = NullIfBlank(request.Email)?.ToLowerInvariant(),

        Notes = NullIfBlank(request.Notes),

        Currency = request.Currency?.Trim().ToUpperInvariant() ?? "EGP"
    };

    /// <summary>
    /// Builds a new <see cref="Player"/> from an already-normalized request.
    /// A straight copy - no trimming, no rules, no surprises.
    ///
    /// <paramref name="enrollmentDate"/> is passed in because resolving the default
    /// needs the clock, and a mapper must stay free of dependencies.
    /// </summary>
    public static Player ToEntity(CreatePlayerRequest request, DateOnly enrollmentDate)
    {
        var nameAr = request.FullNameAr ?? string.Empty;
        var nameEn = request.FullNameEn ?? string.Empty;

        return new Player
        {
            Code = request.Code,

            // A missing translation falls back to the other language instead of
            // rendering blank on the screen.
            FullNameAr = nameAr.Length > 0 ? nameAr : nameEn,
            FullNameEn = nameEn.Length > 0 ? nameEn : nameAr,

            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            NationalId = request.NationalId,

            Sport = request.Sport,
            Level = request.Level,

            GuardianName = request.GuardianName,
            GuardianPhone = request.GuardianPhone,

            Phone = request.Phone,
            Email = request.Email,
            Notes = request.Notes,

            EnrollmentDate = enrollmentDate,
            MonthlyFee = request.MonthlyFee,
            Currency = request.Currency,
            IsActive = true
        };
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // ------------------------------------------------------------------
    // When you add reads, the entity -> DTO mappings go here too:
    //
    // public static PlayerDto ToDto(Player player) => new()
    // {
    //     Id       = player.Id,
    //     Code     = player.Code,
    //     FullName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar"
    //                    ? player.FullNameAr
    //                    : player.FullNameEn,
    //     ...
    // };
    // ------------------------------------------------------------------
}
