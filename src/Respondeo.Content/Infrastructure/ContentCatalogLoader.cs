using Respondeo.Content.Shared;

namespace Respondeo.Content.Infrastructure;

/// <summary>
/// Non-Markdown-specific base for a Discover content service: owns the fetch-cache plumbing every pillar shares regardless of its on-disk format.
/// It exposes the common static-web-asset <see cref="DiscoverRoot"/>, the <see cref="ContentFetcher"/>, manifest fetching,
/// a JSON-manifest loader (for data-driven pillars such as devotions), and the id&#8594;model / ordered-entry catalog projection every pillar's index needs.
/// Markdown pillars layer their front-matter parsing on top via <see cref="MarkdownContentLoader{TFrontMatter, TModel}"/>.
/// </summary>
public abstract class ContentCatalogLoader(ContentFetcher fetcher)
{
    /// <summary>The static-web-asset root every Discover pillar's bundled content is served from.</summary>
    protected const string DiscoverRoot = "_content/Respondeo.Content/discover";

    /// <summary>The fetcher used to retrieve files, exposed so derived loaders can fetch manifests/JSON.</summary>
    protected ContentFetcher Fetcher => fetcher;

    /// <summary>Fetches a pillar's manifest, returning an empty manifest when it is absent/unreachable.</summary>
    protected async Task<ContentManifest> FetchManifestAsync(string manifestPath)
        => await fetcher.GetFromJsonAsync<ContentManifest>(manifestPath) ?? new ContentManifest();

    /// <summary>
    /// Fetches the pillar's manifest from <paramref name="manifestPath"/>, then fetches and maps every listed JSON file (concurrently)
    /// from <paramref name="root"/>, returning the successfully-mapped models in manifest order.
    /// Missing/invalid entries are dropped so a single bad file never takes down the catalog.
    /// </summary>
    protected async Task<IReadOnlyList<TModel>> LoadJsonManifestAsync<TDto, TModel>(string root, string manifestPath, Func<TDto?, TModel?> map)
        where TDto : class
        where TModel : class
    {
        var manifest = await FetchManifestAsync(manifestPath);
        var models = await Task.WhenAll(manifest.Files.Select(f => LoadJsonFileAsync($"{root}/{f}", map)));
        return [.. models.OfType<TModel>()];
    }

    /// <summary>
    /// Builds the two structures every pillar's catalog needs from the loaded models:
    /// an id&#8594;model lookup (for detail pages) and an ordered list of index entries (for the browse/search index), in input order.
    /// </summary>
    protected static (Dictionary<string, TModel> ById, List<TEntry> Entries) BuildCatalog<TModel, TEntry>(
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

    private async Task<TModel?> LoadJsonFileAsync<TDto, TModel>(string url, Func<TDto?, TModel?> map) where TDto : class where TModel : class
    {
        try
        {
            var dto = await fetcher.GetFromJsonAsync<TDto>(url);
            return map(dto);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}
