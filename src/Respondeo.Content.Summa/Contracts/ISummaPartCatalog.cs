namespace Respondeo.Content.Summa.Contracts;

/// <summary>
/// The catalog of Summa parts and the identifier translations the app relies on.
/// Consumers depend on this contract rather than any concrete registry, so the part scheme
/// (storage keys, URL slugs, display labels, folder layout) can change behind it without reworking callers.
///
/// Question ids come in two forms this catalog translates between:
/// <list type="bullet">
///   <item><c>storage id</c>  - <c>{key}-q{n:D3}</c>, e.g. <c>p1-q002</c> (used in files and tokens).</item>
///   <item><c>url id</c>      - <c>{slug}-q{n:D3}</c>, e.g. <c>prima-q002</c> (used in routes/links).</item>
/// </list>
/// </summary>
public interface ISummaPartCatalog
{
    /// <summary>The Summa parts in reading order.</summary>
    IReadOnlyList<SummaPart> All { get; }

    /// <summary>Finds a part by its stable storage key, or null if unknown.</summary>
    SummaPart? ByStorageKey(string key);

    /// <summary>Finds a part by its URL slug, or null if unknown.</summary>
    SummaPart? ByUrlSlug(string slug);

    /// <summary>The on-disk folder that holds a storage part key's question files.</summary>
    string FolderForKey(string key);

    /// <summary>The display label for a storage part key (e.g. <c>p2b</c> -&gt; <c>II-II</c>).</summary>
    string LabelForKey(string key);

    /// <summary>The URL slug for a storage part key (e.g. <c>p1</c> -&gt; <c>prima</c>).</summary>
    string SlugForKey(string key);

    /// <summary>
    /// Translates a storage question id (<c>p1-q002</c>) into its public URL id (<c>prima-q002</c>).
    /// Returns the input unchanged when the part prefix is not recognized.
    /// </summary>
    string ToUrlId(string storageId);

    /// <summary>
    /// Translates a public URL id (<c>prima-q002</c>) back into its storage question id (<c>p1-q002</c>).
    /// Returns the input unchanged when the slug prefix is not recognized.
    /// </summary>
    string ToStorageId(string urlId);

    /// <summary>
    /// Returns the canonical form of a public URL id, or <c>null</c> when it is already canonical or cannot be canonicalised.
    /// Canonicalisation lower-cases a known slug and zero-pads the question number to three digits, so a hand-typed <c>Prima-q1</c> maps to <c>prima-q001</c>.
    /// Returns <c>null</c> when the slug is unknown or the id is already in canonical form, so callers can cheaply decide whether a redirect is needed.
    /// </summary>
    string? Canonicalize(string? urlId);
}
