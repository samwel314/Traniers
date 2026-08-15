namespace ERP.Application.Common.Exceptions;

/// <summary>
/// Input that failed FluentValidation. Carries a per-field dictionary so the API
/// can return a proper ValidationProblemDetails.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures occurred.")
        => Errors = errors;

    public IDictionary<string, string[]> Errors { get; }
}

/// <summary>The caller is authenticated but lacks the required permission.</summary>
public sealed class ForbiddenAccessException(string permission)
    : Exception($"Missing permission: {permission}")
{
    public string Permission { get; } = permission;
}
