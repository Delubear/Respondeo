using Respondeo.Content.Shared;

namespace Respondeo.Content.Infrastructure;

/// <summary>
/// Generic base for a content service that loads a set of Markdown files, each carrying a "---" delimited YAML front-matter block, into typed models.
/// Owns the repeated fetch-cache-parse plumbing every pillar would otherwise hand-roll: it fetches files (concurrently) through a <see cref="ContentFetcher"/>,
/// reads their front matter through a <see cref="FrontMatterReader"/>, tolerates a single missing file, and defers the front-matter-to-model mapping to <see cref="Map"/>.
/// Pillars whose catalog is a single model type derive from this directly;
/// pillars with several model types (or no front matter) compose <see cref="ContentFetcher"/>/<see cref="FrontMatterReader"/> directly instead.
/// </summary>
/// <typeparam name="TFrontMatter">The pillar's front-matter DTO.</typeparam>
/// <typeparam name="TModel">The materialized public model.</typeparam>
public abstract class MarkdownContentLoader<TFrontMatter, TModel>(ContentFetcher fetcher, FrontMatterReader reader)
    where TFrontMatter : ContentFrontMatterBase
    where TModel : class
{
    /// <summary>The static-web-asset root every Discover pillar's bundled content is served from.</summary>
    protected const string DiscoverRoot = "_content/Respondeo.Content/discover";

    /// <summary>The fetcher used to retrieve files, exposed so derived loaders can also fetch manifests/JSON.</summary>
    protected ContentFetcher Fetcher => fetcher;

    /// <summary>
    /// Maps a successfully-parsed front-matter block and Markdown body onto the public model.
    /// The <paramref name="fileName"/> is the manifest entry the content came from, so derived loaders can derive per-file context (e.g. a stage from the containing folder).
    /// </summary>
    protected abstract TModel Map(TFrontMatter meta, string body, string fileName);

    /// <summary>
    /// Fetches the pillar's manifest from <paramref name="manifestPath"/>, then fetches and parses every listed file
    /// (concurrently) from <paramref name="root"/>, returning the successfully-parsed models in manifest order.
    /// Missing/invalid entries are dropped so a single bad file never takes down the catalog.
    /// </summary>
    protected async Task<IReadOnlyList<TModel>> LoadManifestAsync(string root, string manifestPath)
    {
        var manifest = await fetcher.GetFromJsonAsync<ContentManifest>(manifestPath) ?? new ContentManifest();
        var parsed = await LoadFilesAsync(manifest.Files.Select(f => ($"{root}/{f}", f)));
        return [.. parsed.OfType<TModel>()];
    }

    /// <summary>
    /// Builds the two structures every pillar's catalog needs from the loaded models: an id&#8594;model lookup
    /// (for detail pages) and an ordered list of index entries (for the browse/search index), in input order.
    /// </summary>
    protected static (Dictionary<string, TModel> ById, List<TEntry> Entries) BuildCatalog<TEntry>(
        IEnumerable<TModel> models,
        Func<TModel, string> idSelector,
        Func<TModel, TEntry> entrySelector)
    {
        var byId = new Dictionary<string, TModel>(StringComparer.OrdinalIgnoreCase);
        var entries = new List<TEntry>();
        foreach (var model in models)
        {
            byId[idSelector(model)] = model;
            entries.Add(entrySelector(model));
        }

        return (byId, entries);
    }

    /// <summary>
    /// Fetches and converts a pillar's <c>facets.json</c> into its catalog, returning <paramref name="empty"/> when the
    /// file is absent/unreachable so a missing facets file never breaks browsing (labels fall back to humanized slugs).
    /// </summary>
    protected async Task<TCatalog> LoadFacetsAsync<TDto, TCatalog>(string path, Func<TDto, TCatalog> toCatalog, TCatalog empty)
        where TDto : class
    {
        try
        {
            var dto = await fetcher.GetFromJsonAsync<TDto>(path);
            return dto is null ? empty : toCatalog(dto);
        }
        catch (HttpRequestException)
        {
            return empty;
        }
    }

    /// <summary>
    /// Fetches and parses a single Markdown file into its model, or returns null when the file is missing/unreachable
    /// (e.g. a stale static-web-asset manifest returning 404) or its front matter is invalid.
    /// A single absent file never takes down the rest of the catalog.
    /// </summary>
    protected async Task<TModel?> LoadFileAsync(string url, string fileName)
    {
        try
        {
            var raw = await fetcher.GetStringAsync(url);
            return reader.TryRead<TFrontMatter>(raw, out var meta, out var body) ? Map(meta!, body, fileName) : null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <summary>
    /// Fetches and parses every supplied file concurrently, preserving input order in the result.
    /// Fetching in parallel overlaps the network round trips so a cold load of dozens of small files does not serialise into a noticeable first-load delay;
    /// failed/invalid entries surface as null.
    /// </summary>
    protected Task<TModel?[]> LoadFilesAsync(IEnumerable<(string Url, string FileName)> files) => Task.WhenAll(files.Select(f => LoadFileAsync(f.Url, f.FileName)));
}
