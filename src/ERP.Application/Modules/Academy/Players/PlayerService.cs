using ERP.Application.Common.Abstractions.Persistence;
using ERP.Application.Common.Abstractions.Services;
using ERP.Application.Common.Security;
using ERP.Application.Common.Services;
using ERP.Application.Modules.Academy.Players.Contracts;
using ERP.Application.Modules.Academy.Players.Mapping;
using ERP.Domain.Common.Results;
using ERP.Domain.Modules.Academy;
using Microsoft.Extensions.Logging;

namespace ERP.Application.Modules.Academy.Players;

/// <summary>
/// Everything you can do with a player lives here: the rules, the database calls,
/// and the order they happen in.
///
/// The method reads top to bottom:
///   1. is the caller allowed?      guard.Require(...)
///   2. is the input well formed?   validator.ValidateAsync(...)
///   3. do the academy rules allow it?   plain if statements
///   4. build the row and save      unitOfWork.SaveChangesAsync(...)
/// </summary>
internal sealed class PlayerService(
    IPlayerRepository players,
    IUnitOfWork unitOfWork,
    IInputValidator validator,
    IPermissionGuard guard,
    IDateTimeProvider dateTime,
    ILogger<PlayerService> logger) : IPlayerService
{
    /// <summary>The academy only accepts players in this age range.</summary>
    private const int MinimumAge = 4;
    private const int MaximumAge = 45;

    /// <summary>Below this age the academy requires a guardian on file.</summary>
    private const int AdultAge = 18;

    public async Task<Result<Guid>> CreateAsync(
        CreatePlayerRequest request,
        CancellationToken cancellationToken = default)
    {
        guard.Require(Permissions.Academy.CreatePlayer);

        // Clean the input first, so the validator judges the same values that will
        // actually be stored - a pasted "  PL-0001  " must not fail on whitespace.
        request = PlayerMapper.Normalize(request);

        await validator.ValidateAsync(request, cancellationToken);

        // ------------------------------------------------------------------
        // 3. Academy rules
        // ------------------------------------------------------------------
        var today = dateTime.Today;

        if (request.DateOfBirth > today)
            return Result.Failure<Guid>(AcademyErrors.BirthDateInFuture);

        var age = CalculateAge(request.DateOfBirth, today);

        if (age < MinimumAge || age > MaximumAge)
            return Result.Failure<Guid>(
                AcademyErrors.AgeOutOfRange.WithArgs(MinimumAge, MaximumAge));

        // A minor must have a guardian the academy can call.
        if (age < AdultAge &&
            (string.IsNullOrWhiteSpace(request.GuardianName) ||
             string.IsNullOrWhiteSpace(request.GuardianPhone)))
        {
            return Result.Failure<Guid>(AcademyErrors.GuardianRequired);
        }

        var enrollmentDate = request.EnrollmentDate ?? today;

        if (enrollmentDate > today)
            return Result.Failure<Guid>(AcademyErrors.EnrollmentDateInFuture);

        if (request.MonthlyFee < 0)
            return Result.Failure<Guid>(AcademyErrors.FeeNegative);

        // Uniqueness is a rule about the *set* of players, so it needs the database.
        // request is already normalized, so these compare the stored form.
        if (await players.CodeExistsAsync(request.Code, cancellationToken))
            return Result.Failure<Guid>(AcademyErrors.CodeAlreadyExists);

        if (request.NationalId is not null &&
            await players.NationalIdExistsAsync(request.NationalId, cancellationToken))
        {
            return Result.Failure<Guid>(AcademyErrors.NationalIdAlreadyExists);
        }

        // ------------------------------------------------------------------
        // 4. Map, then save
        // ------------------------------------------------------------------
        var player = PlayerMapper.ToEntity(request, enrollmentDate);

        players.Add(player);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Player {Code} enrolled in {Sport} ({PlayerId}), age {Age}",
            player.Code, player.Sport, player.Id, age);

        return Result.Success(player.Id);
    }

    /// <summary>
    /// Full years only. Subtracting the years is not enough - someone born in
    /// December is not a year older until December comes round again.
    /// </summary>
    private static int CalculateAge(DateOnly dateOfBirth, DateOnly today)
    {
        var age = today.Year - dateOfBirth.Year;

        if (dateOfBirth > today.AddYears(-age))
            age--;

        return age;
    }
}
