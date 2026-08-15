using ERP.Application.Common.Abstractions.Services;
using FluentValidation;

namespace ERP.Application.Common.Validation;

/// <summary>
/// Base class for command/query validators that keeps messages out of the code.
/// Instead of a hard-coded English string you pass a resource key:
///
/// <code>
/// RuleFor(x => x.Name).NotEmpty().WithLocalizedMessage(this, "Validation.Required");
/// </code>
///
/// The message is resolved per request against the caller's culture.
/// </summary>
public abstract class LocalizedValidator<TRequest> : AbstractValidator<TRequest>
{
    protected LocalizedValidator(ITranslator translator) => Translator = translator;

    protected ITranslator Translator { get; }

    /// <summary>Shorthand for a translated string inside a custom rule.</summary>
    protected string Localize(string key, params object[] args) => Translator.Get(key, args);
}

public static class LocalizedValidatorExtensions
{
    public static IRuleBuilderOptions<T, TProperty> WithLocalizedMessage<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule,
        ITranslator translator,
        string key,
        params object[] args)
        => rule.WithMessage(_ => translator.Get(key, args));
}

/// <summary>Keys that ship with the skeleton. Add your own next to them in ar.json / en.json.</summary>
public static class ValidationKeys
{
    public const string Required = "Validation.Required";
    public const string MaxLength = "Validation.MaxLength";
    public const string MinLength = "Validation.MinLength";
    public const string GreaterThanZero = "Validation.GreaterThanZero";
    public const string InvalidFormat = "Validation.InvalidFormat";
    public const string InvalidEmail = "Validation.InvalidEmail";
    public const string NotFound = "Validation.NotFound";
    public const string AlreadyExists = "Validation.AlreadyExists";
}
