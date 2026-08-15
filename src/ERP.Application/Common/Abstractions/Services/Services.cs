using System.Globalization;

namespace ERP.Application.Common.Abstractions.Services;

/// <summary>Time is a dependency. Handlers that call DateTime.UtcNow cannot be tested.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
    DateOnly Today { get; }
}

/// <summary>Who is making this request. Implemented over HttpContext in Infrastructure.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    Guid? TenantId { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
    bool HasPermission(string permission);
}

/// <summary>
/// The Application layer's whole view of localization.
/// It knows keys and cultures; it does not know that the translations happen to
/// live in JSON files today - that is an Infrastructure detail, swappable for a
/// database table without touching a single use case.
/// </summary>
public interface ITranslator
{
    string this[string key] { get; }
    string Get(string key, params object[] args);
    string GetFor(CultureInfo culture, string key, params object[] args);
    CultureInfo CurrentCulture { get; }
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}

/// <summary>Correlates every log line, response header and audit row of one request.</summary>
public interface IRequestContext
{
    string CorrelationId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}
