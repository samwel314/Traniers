using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERP.Infrastructure.Localization;

/// <summary>
/// Loads translations from plain JSON files - one file per culture
/// (<c>Localization/Resources/ar.json</c>, <c>en.json</c>).
///
/// Why JSON instead of .resx:
///   - a translator can edit it without Visual Studio and without a rebuild,
///   - it diffs cleanly in git, unlike the XML schema block resx carries around,
///   - the same files can be handed to the front-end so both ends share one glossary.
///
/// Files are read once, cached, and reloaded automatically when changed on disk.
/// </summary>
public sealed class JsonTranslationStore : IDisposable
{
    private readonly ErpLocalizationOptions _options;
    private readonly ILogger<JsonTranslationStore> _logger;
    private readonly string _root;

    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _cache = new();
    private readonly FileSystemWatcher? _watcher;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public JsonTranslationStore(
        IOptions<ErpLocalizationOptions> options,
        ILogger<JsonTranslationStore> logger)
    {
        _options = options.Value;
        _logger = logger;
        _root = Path.IsPathRooted(_options.ResourcesPath)
            ? _options.ResourcesPath
            : Path.Combine(AppContext.BaseDirectory, _options.ResourcesPath);

        if (!Directory.Exists(_root))
        {
            _logger.LogWarning("Localization directory {Path} does not exist - every key will fall back.", _root);
            return;
        }

        if (!_options.ReloadOnChange)
            return;

        // Lets ops fix a wrong wording in production by editing a file,
        // instead of waiting for a redeploy.
        _watcher = new FileSystemWatcher(_root, "*.json")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Renamed += OnFileChanged;
    }

    /// <summary>
    /// Resolves a key for a culture, walking the fallback chain:
    /// <c>ar-EG</c> → <c>ar</c> → the configured default culture.
    /// Returns null when the key exists nowhere, so the caller decides what to show.
    /// </summary>
    public string? Find(string culture, string key)
    {
        foreach (var candidate in FallbackChain(culture))
        {
            if (Load(candidate).TryGetValue(key, out var value))
                return value;
        }

        return null;
    }

    private IEnumerable<string> FallbackChain(string culture)
    {
        if (!string.IsNullOrWhiteSpace(culture))
        {
            yield return culture;

            // "ar-EG" also tries "ar"
            var dash = culture.IndexOf('-');
            if (dash > 0)
                yield return culture[..dash];
        }

        if (!string.Equals(culture, _options.DefaultCulture, StringComparison.OrdinalIgnoreCase))
            yield return _options.DefaultCulture;
    }

    private IReadOnlyDictionary<string, string> Load(string culture)
        => _cache.GetOrAdd(culture, ReadFile);

    private IReadOnlyDictionary<string, string> ReadFile(string culture)
    {
        var path = Path.Combine(_root, $"{culture}.json");

        if (!File.Exists(path))
            return new Dictionary<string, string>();

        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var document = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            var flat = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Flatten(document.RootElement, prefix: null, flat);

            _logger.LogInformation("Loaded {Count} translations for {Culture}", flat.Count, culture);
            return flat;
        }
        catch (Exception ex)
        {
            // A malformed translation file must never take the API down.
            _logger.LogError(ex, "Failed to read localization file {Path}", path);
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Turns nested objects into dotted keys, so the file can be written either way:
    /// <c>{ "Validation": { "Required": "..." } }</c> and <c>{ "Validation.Required": "..." }</c>
    /// both resolve as <c>Validation.Required</c>.
    /// </summary>
    private static void Flatten(JsonElement element, string? prefix, IDictionary<string, string> target)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = prefix is null ? property.Name : $"{prefix}.{property.Name}";
                    Flatten(property.Value, key, target);
                }
                break;

            case JsonValueKind.Array:
                // Joined so a multi-line message can be written as a JSON array.
                if (prefix is not null)
                    target[prefix] = string.Join(" ", element.EnumerateArray().Select(e => e.ToString()));
                break;

            case JsonValueKind.Null or JsonValueKind.Undefined:
                break;

            default:
                if (prefix is not null)
                    target[prefix] = element.ToString();
                break;
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var culture = Path.GetFileNameWithoutExtension(e.Name);
        if (string.IsNullOrWhiteSpace(culture))
            return;

        _cache.TryRemove(culture, out _);
        _logger.LogInformation("Localization file for {Culture} changed - cache invalidated.", culture);
    }

    /// <summary>Drops every cached culture. Useful after a bulk translation import.</summary>
    public void Reload() => _cache.Clear();

    public void Dispose()
    {
        if (_watcher is null) return;

        _watcher.Changed -= OnFileChanged;
        _watcher.Created -= OnFileChanged;
        _watcher.Renamed -= OnFileChanged;
        _watcher.Dispose();
    }
}

/// <summary>Bound from the "Localization" configuration section.</summary>
public sealed class ErpLocalizationOptions
{
    public const string SectionName = "Localization";

    /// <summary>Relative to the application base directory unless rooted.</summary>
    public string ResourcesPath { get; set; } = "Localization/Resources";

    public string DefaultCulture { get; set; } = "ar";

    public string[] SupportedCultures { get; set; } = ["ar", "en"];

    /// <summary>Pick up edits to the JSON files without restarting the app.</summary>
    public bool ReloadOnChange { get; set; } = true;
}
