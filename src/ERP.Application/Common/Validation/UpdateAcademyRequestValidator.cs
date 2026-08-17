using ERP.Application.Common.Abstractions.Services;
using ERP.Application.Modules.Academy.AcademyInput;
using FluentValidation;

namespace ERP.Application.Common.Validation;

public class UpdateAcademyRequestValidator
: LocalizedValidator<UpdateAcademyRequest>
{
    public UpdateAcademyRequestValidator(ITranslator translator)
    : base(translator)
    {
        RuleFor(x => x.NameEn)
        .NotEmpty()
        .WithLocalizedMessage(
        Translator,
        ValidationKeys.Required)
        .MaximumLength(100)
        .WithLocalizedMessage(
        Translator,
        ValidationKeys.MaxLength);


    RuleFor(x => x.NameAr)
        .NotEmpty()
        .WithLocalizedMessage(
            Translator,
            ValidationKeys.Required)
        .MaximumLength(100)
        .WithLocalizedMessage(
            Translator,
            ValidationKeys.MaxLength);

        RuleFor(x => x.DescriptionEn)
            .MaximumLength(1000)
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.MaxLength);

        RuleFor(x => x.DescriptionAr)
            .MaximumLength(1000)
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.MaxLength);

        RuleFor(x => x.SportId)
            .NotEmpty()
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.Required);

        RuleFor(x => x.CityId)
            .NotEmpty()
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.Required);

        RuleFor(x => x.AreaId)
            .NotEmpty()
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.Required);

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.Required)
            .MaximumLength(500)
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.MaxLength);

        RuleFor(x => x.FirstPhone)
            .NotEmpty()
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.Required)
            .MaximumLength(20)
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.MaxLength)
            .Matches(@"^\+?[0-9\s\-()]+$")
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.InvalidFormat);

        RuleFor(x => x.SecondPhone)
            .MaximumLength(20)
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.MaxLength)
            .Matches(@"^\+?[0-9\s\-()]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.SecondPhone))
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.InvalidFormat);

        RuleFor(x => x.Email)
            .EmailAddress()
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.InvalidEmail)
            .MaximumLength(256)
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.WebsiteUrl)
            .MaximumLength(500)
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.MaxLength)
            .Must(url =>
                string.IsNullOrWhiteSpace(url) ||
                (Uri.TryCreate(url, UriKind.Absolute, out var uri)
                 && uri.Scheme == Uri.UriSchemeHttps))
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.InvalidFormat);

        RuleFor(x => x.FacebookUrl)
            .MaximumLength(500)
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.MaxLength)
            .Must(url =>
                string.IsNullOrWhiteSpace(url) ||
                (Uri.TryCreate(url, UriKind.Absolute, out var uri)
                 && uri.Scheme == Uri.UriSchemeHttps))
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.InvalidFormat);

        RuleFor(x => x.InstagramUrl)
            .MaximumLength(500)
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.MaxLength)
            .Must(url =>
                string.IsNullOrWhiteSpace(url) ||
                (Uri.TryCreate(url, UriKind.Absolute, out var uri)
                 && uri.Scheme == Uri.UriSchemeHttps))
            .WithLocalizedMessage(
                Translator,
                ValidationKeys.InvalidFormat);
    }

}
