using ERP.Application.Common.Abstractions.Services;
using ERP.Application.Common.Validation;
using ERP.Domain.Modules.Academy;
using FluentValidation;

namespace ERP.Application.Modules.Academy.Players.Contracts;

/// <summary>
/// What the client sends to enrol a player. A record, so nothing can change it
/// halfway through the service method.
/// </summary>
public sealed record CreatePlayerRequest
{
    public string Code { get; init; } = string.Empty;
    public string? FullNameAr { get; init; }
    public string? FullNameEn { get; init; }

    public DateOnly DateOfBirth { get; init; }
    public Gender Gender { get; init; }
    public string? NationalId { get; init; }

    public Sport Sport { get; init; }
    public PlayerLevel Level { get; init; }

    public string GuardianName { get; init; } = string.Empty;
    public string GuardianPhone { get; init; } = string.Empty;

    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Notes { get; init; }

    public DateOnly? EnrollmentDate { get; init; }
    public decimal MonthlyFee { get; init; }
    public string Currency { get; init; } = "EGP";
}

/// <summary>
/// Shape of the input only - "is this a well-formed request?", never
/// "is this allowed by the academy's rules?". The second question is the service's.
/// Messages are translation keys, so a bad request reads correctly in Arabic.
/// </summary>
internal sealed class CreatePlayerRequestValidator : LocalizedValidator<CreatePlayerRequest>
{
    public CreatePlayerRequestValidator(ITranslator translator) : base(translator)
    {
        // These rules run on the *normalized* request - PlayerService calls
        // PlayerMapper.Normalize(request) before validating, so a pasted
        // "  PL-0001  " is trimmed before it is judged.
        RuleFor(x => x.Code)
            .NotEmpty().WithLocalizedMessage(translator, ValidationKeys.Required)
            .MaximumLength(32).WithLocalizedMessage(translator, ValidationKeys.MaxLength, 32)
            .Matches("^[a-zA-Z0-9][a-zA-Z0-9-]{2,31}$")
                .WithLocalizedMessage(translator, "Academy.Player.CodeInvalidFormat");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.FullNameAr) || !string.IsNullOrWhiteSpace(x.FullNameEn))
            .WithName(nameof(CreatePlayerRequest.FullNameAr))
            .WithLocalizedMessage(translator, ValidationKeys.Required);

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithLocalizedMessage(translator, ValidationKeys.Required);

        RuleFor(x => x.GuardianName)
            .NotEmpty().WithLocalizedMessage(translator, ValidationKeys.Required)
            .MaximumLength(200).WithLocalizedMessage(translator, ValidationKeys.MaxLength, 200);

        RuleFor(x => x.GuardianPhone)
            .NotEmpty().WithLocalizedMessage(translator, ValidationKeys.Required)
            // Egyptian mobile: 01 followed by 0/1/2/5 and 8 more digits.
            .Matches(@"^01[0125][0-9]{8}$")
                .WithLocalizedMessage(translator, "Academy.Player.PhoneInvalidFormat");

        RuleFor(x => x.Phone)
            .Matches(@"^01[0125][0-9]{8}$")
                .WithLocalizedMessage(translator, "Academy.Player.PhoneInvalidFormat")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Email)
            .EmailAddress().WithLocalizedMessage(translator, ValidationKeys.InvalidEmail)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.NationalId)
            .Length(14).WithLocalizedMessage(translator, "Academy.Player.NationalIdInvalidFormat")
            .Matches("^[0-9]+$").WithLocalizedMessage(translator, "Academy.Player.NationalIdInvalidFormat")
            .When(x => !string.IsNullOrWhiteSpace(x.NationalId));

        RuleFor(x => x.MonthlyFee)
            .GreaterThanOrEqualTo(0).WithLocalizedMessage(translator, ValidationKeys.GreaterThanZero);

        RuleFor(x => x.Currency)
            .NotEmpty().WithLocalizedMessage(translator, ValidationKeys.Required)
            .Length(3).WithLocalizedMessage(translator, ValidationKeys.InvalidFormat);

        RuleFor(x => x.Sport)
            .IsInEnum().WithLocalizedMessage(translator, ValidationKeys.InvalidFormat);

        RuleFor(x => x.Level)
            .IsInEnum().WithLocalizedMessage(translator, ValidationKeys.InvalidFormat);

        RuleFor(x => x.Gender)
            .IsInEnum().WithLocalizedMessage(translator, ValidationKeys.InvalidFormat);
    }
}
