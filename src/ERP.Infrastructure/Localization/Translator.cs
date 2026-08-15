using System.Globalization;
using ERP.Application.Common.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Localization;

/// <summary>
/// The only place in the solution that knows where translations come from.
/// Swapping the JSON files for a database table means rewriting this class and
/// <see cref="JsonTranslationStore"/> - and nothing else, because every caller
/// depends on <see cref="ITranslator"/>.
///
/// A missing key is logged and returned as-is instead of throwing: a half-translated
/// screen is bad, a 500 caused by a missing translation is worse.
/// </summary>
public sealed class Translator(JsonTranslationStore store, ILogger<Translator> logger) : ITranslator
{
    public string this[string key] => Get(key);

    public CultureInfo CurrentCulture => CultureInfo.CurrentUICulture;

    public string Get(string key, params object[] args)
        => GetFor(CurrentCulture, key, args);

    /// <summary>
    /// Translates into an explicit culture regardless of the current request -
    /// needed for emails, printed documents and background jobs that must render
    /// in the recipient's language, not the server's.
    /// </summary>
    public string GetFor(CultureInfo culture, string key, params object[] args)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        var value = store.Find(culture.Name, key);

        if (value is null)
        {
            logger.LogDebug("Missing translation for {Key} in {Culture}", key, culture.Name);
            return key; // callers treat "value == key" as "not translated yet"
        }

        if (args.Length == 0)
            return value;

        try
        {
            return string.Format(culture, value, args);
        }
        catch (FormatException ex)
        {
            // A placeholder mismatch between ar.json and en.json must not throw
            // in one language only - return the raw text instead.
            logger.LogWarning(ex, "Bad format placeholders for {Key} in {Culture}", key, culture.Name);
            return value;
        }
    }
}
