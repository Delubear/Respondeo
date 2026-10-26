using System.Net.Http.Json;
using System.Text.Json;

namespace Respondeo.Bible;

/// <summary>
/// Loads the translations listed in <c>translations.json</c> together with each translation's <c>manifest.json</c>,
/// lazily on first use and cached for the lifetime of the scope.
/// </summary>
public sealed class BibleTranslationCatalog(HttpClient http) : IBibleTranslations
{
    internal const string DataRoot = "_content/Respondeo.Bible";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    private Task<IReadOnlyList<BibleTranslation>>? _load;

    public Task<IReadOnlyList<BibleTranslation>> GetAllAsync() => _load ??= LoadAsync();

    public async Task<BibleTranslation> GetDefaultAsync() => (await GetAllAsync())[0];

    public async Task<BibleTranslation?> FindAsync(string? code) => (await GetAllAsync()).FirstOrDefault(t => t.Matches(code));

    /// <summary>Loads every translation from a <c>wwwroot</c> directory on disk (tools and tests).</summary>
    public static IReadOnlyList<BibleTranslation> LoadFromDirectory(string wwwrootPath)
    {
        var index = Deserialize<List<IndexEntry>>(File.ReadAllText(Path.Combine(wwwrootPath, "translations.json")));
        return [.. index.Select(entry => Create(
            entry,
            Deserialize<Manifest>(File.ReadAllText(Path.Combine(wwwrootPath, entry.Folder, "manifest.json")))))];
    }

    private async Task<IReadOnlyList<BibleTranslation>> LoadAsync()
    {
        var index = await http.GetFromJsonAsync<List<IndexEntry>>($"{DataRoot}/translations.json", Options) ?? [];
        var manifests = await Task.WhenAll(index.Select(entry => http.GetFromJsonAsync<Manifest>($"{DataRoot}/{entry.Folder}/manifest.json", Options)));
        return [.. index.Select((entry, i) => Create(entry, manifests[i]))];
    }

    private static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options) ?? throw new InvalidOperationException($"Could not read {typeof(T).Name}.");

    private static BibleTranslation Create(IndexEntry entry, Manifest? manifest) => new(
        entry.Code,
        entry.Name,
        entry.ShortName ?? entry.Name,
        entry.Folder,
        entry.Description,
        entry.Aliases ?? [],
        new Dictionary<string, BibleTranslationBook>(manifest?.Books ?? [], StringComparer.Ordinal));

    private sealed record IndexEntry(string Code, string Name, string? ShortName, string Folder, string? Description, string[]? Aliases);

    private sealed record Manifest(Dictionary<string, BibleTranslationBook>? Books);
}
