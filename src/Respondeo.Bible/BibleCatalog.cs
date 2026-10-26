using System.Net.Http.Json;
using System.Text.Json;

namespace Respondeo.Bible;

/// <summary>
/// The per-translation book manifests (<c>wwwroot/{folder}/manifest.json</c>), which define which books a translation
/// contains, what it names them, how many chapters each has, and the abbreviations it accepts.
/// </summary>
public static class BibleCatalog
{
    internal const string DataRoot = "_content/Respondeo.Bible";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    private static readonly Dictionary<BibleTranslation, IReadOnlyDictionary<string, ManifestEntry>> Manifests = [];

    /// <summary>Loads every translation's manifest over HTTP from the app's static web assets.</summary>
    public static async Task LoadAsync(HttpClient http)
    {
        foreach (var translation in BibleTranslations.All)
        {
            var manifest = await http.GetFromJsonAsync<Manifest>($"{DataRoot}/{translation.Folder()}/manifest.json", Options);
            Set(translation, manifest);
        }
    }

    /// <summary>Loads every translation's manifest from a <c>wwwroot</c> directory on disk (tools and tests).</summary>
    public static void LoadFromDirectory(string wwwrootPath)
    {
        foreach (var translation in BibleTranslations.All)
        {
            var json = File.ReadAllText(Path.Combine(wwwrootPath, translation.Folder(), "manifest.json"));
            Set(translation, JsonSerializer.Deserialize<Manifest>(json, Options));
        }
    }

    /// <summary>Whether the manifest for the translation has been loaded.</summary>
    public static bool IsLoaded(BibleTranslation translation) => Manifests.ContainsKey(translation);

    internal static ManifestEntry? Entry(BibleTranslation translation, BibleBook book) =>
        Books(translation).TryGetValue(book.Slug, out var entry) ? entry : null;

    internal static IReadOnlyDictionary<string, ManifestEntry> Books(BibleTranslation translation) =>
        Manifests.TryGetValue(translation, out var books)
            ? books
            : throw new InvalidOperationException($"The {translation} manifest has not been loaded; call BibleCatalog.LoadAsync at startup.");

    private static void Set(BibleTranslation translation, Manifest? manifest) =>
        Manifests[translation] = new Dictionary<string, ManifestEntry>(manifest?.Books ?? [], StringComparer.Ordinal);

    internal sealed record Manifest(Dictionary<string, ManifestEntry>? Books);

    internal sealed record ManifestEntry(string? File, string? Name, int? Chapters, string[]? Abbreviations);
}
