using ERP.Application.Common.Abstractions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace ERP.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}

/// <summary>
/// Per-request facts that every log line and audit row wants.
/// The correlation id is set by the middleware and echoed back in a response header,
/// so a user reporting "it failed at 2:15" hands you one id that finds every log line.
/// </summary>
public sealed class RequestContext(IHttpContextAccessor accessor) : IRequestContext
{
    public const string HeaderName = "X-Correlation-Id";

    public string CorrelationId =>
        accessor.HttpContext?.Items[HeaderName] as string
        ?? accessor.HttpContext?.TraceIdentifier
        ?? "no-http-context";

    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => accessor.HttpContext?.Request.Headers.UserAgent.ToString();
}

/// <summary>
/// In-memory cache with prefix invalidation. Swap this single class for a Redis
/// implementation when you scale out - nothing else in the solution changes.
/// </summary>
public sealed class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    // IMemoryCache cannot enumerate its keys, so we track what we put in.
    private readonly HashSet<string> _keys = [];
    private readonly Lock _gate = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(cache.TryGetValue(key, out T? value) ? value : default);

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        cache.Set(key, value, expiration ?? DefaultExpiration);

        lock (_gate)
            _keys.Add(key);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cache.Remove(key);

        lock (_gate)
            _keys.Remove(key);

        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        List<string> matching;

        lock (_gate)
            matching = _keys.Where(k => k.Contains(prefix, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var key in matching)
        {
            cache.Remove(key);
            lock (_gate)
                _keys.Remove(key);
        }

        return Task.CompletedTask;
    }
}
