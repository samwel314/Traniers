using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ValidationException = ERP.Application.Common.Exceptions.ValidationException;

namespace ERP.Application.Common.Services;

/// <summary>
/// Validation used to be a pipeline behavior. Without a mediator there is no
/// pipeline, so a service calls this explicitly as its first line:
///
/// <code>await validator.ValidateAsync(request, ct);</code>
///
/// It throws <see cref="ValidationException"/>, which the API's global exception
/// handler turns into a localized 400 with per-field errors - exactly the response
/// the behavior used to produce.
/// </summary>
public interface IInputValidator
{
    /// <summary>Throws if the input is malformed. No validator registered = nothing to check.</summary>
    Task ValidateAsync<T>(T instance, CancellationToken cancellationToken = default);
}

internal sealed class InputValidator(IServiceProvider serviceProvider) : IInputValidator
{
    public async Task ValidateAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        var validators = serviceProvider.GetServices<IValidator<T>>().ToList();

        if (validators.Count == 0)
            return;

        var context = new ValidationContext<T>(instance!);

        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count == 0)
            return;

        var errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).Distinct().ToArray());

        throw new ValidationException(errors);
    }
}
