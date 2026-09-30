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
    /// <summary>The fetcher used to retrieve files, exposed so derived loaders can also fetch manifests/JSON.</summary>
    protected ContentFetcher Fetcher => fetcher;

    /// <summary>
    /// Maps a successfully-parsed front-matter block and Markdown body onto the public model.
    /// The <paramref name="fileName"/> is the manifest entry the content came from, so derived loaders can derive per-file context (e.g. a stage from the containing folder).
    /// </summary>
    protected abstract TModel Map(TFrontMatter meta, string body, string fileName);

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
