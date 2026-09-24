using System.Text.RegularExpressions;

namespace Respondeo.Content.Summa;

/// <summary>
/// One part of the Summa and the several identifiers we attach to it.
/// </summary>
/// <param name="Key">
/// The stable, neutral storage key baked into the corpus:
/// question ids, JSON filenames, and the <c>sref</c>/<c>sobj</c> cross-reference tokens all use this (e.g. <c>p1</c>, <c>p2a</c>).
/// It never changes, so the slug and label below can be re-styled freely without regenerating the corpus.
/// </param>
/// <param name="Slug">The URL segment shown to readers (e.g. <c>prima</c>): <c>/summa/prima-q002</c>.</param>
/// <param name="Label">The short display label used in cross-references (e.g. <c>II-II</c>).</param>
/// <param name="Title">The human-readable part title (e.g. <c>First Part</c>).</param>
/// <param name="Folder">The on-disk corpus subfolder holding this part's question files.</param>
/// <param name="LatinName">The traditional Latin name of the part (e.g. <c>Prima Pars</c>).</param>
/// <param name="Description">A short plain-language summary of what the part covers.</param>
public sealed record SummaPart(
    string Key,
    string Slug,
    string Label,
    string Title,
    string Folder,
    string LatinName,
    string Description);

/// <summary>
/// The single source of truth mapping a Summa part between its stable storage key, its URL slug,
/// its display label, its title, and its on-disk folder.
/// Storage keys are permanent; the slug and label are presentation concerns that can change without touching the generated corpus.
///
/// Question ids come in two forms that this type translates between:
/// <list type="bullet">
///   <item><c>storage id</c>  - <c>{key}-q{n:D3}</c>, e.g. <c>p1-q002</c> (used in files and tokens).</item>
///   <item><c>url id</c>      - <c>{slug}-q{n:D3}</c>, e.g. <c>prima-q002</c> (used in routes/links).</item>
/// </list>
/// </summary>
public static partial class SummaParts
{
    // Reading order. Key = permanent storage id; Slug = URL form; Label = cross-reference display.
    public static IReadOnlyList<SummaPart> All { get; } =
    [
        new("p1", "prima", "I", "First Part", "first-part", "Prima Pars",
            "God, the Trinity, creation, the angels, and the nature of man."),
        new("p2a", "primsec", "I-II", "First Part of the Second Part", "first-part-of-the-second-part", "Prima Secundae",
            "Human happiness and the general principles of morality: acts, passions, habits, virtues, sin, law, and grace."),
        new("p2b", "secsec", "II-II", "Second Part of the Second Part", "second-part-of-the-second-part", "Secunda Secundae",
            "Morality in particular: the theological and cardinal virtues with their opposing vices, and states of life."),
        new("p3", "tertia", "III", "Third Part", "third-part", "Tertia Pars",
            "Christ the Saviour, his Incarnation and life, and the sacraments through which grace reaches us."),
        new("sup", "suppl", "Suppl.", "Supplement", "supplement", "Supplementum",
            "Compiled after Aquinas's death, completing the sacraments (penance, orders, matrimony) and the last things."),
    ];

    private static readonly Dictionary<string, SummaPart> ByKey = All.ToDictionary(p => p.Key, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, SummaPart> BySlug = All.ToDictionary(p => p.Slug, StringComparer.OrdinalIgnoreCase);

    /// <summary>Finds a part by its stable storage key, or null if unknown.</summary>
    public static SummaPart? ByStorageKey(string key) => ByKey.GetValueOrDefault(key);

    /// <summary>Finds a part by its URL slug, or null if unknown.</summary>
    public static SummaPart? ByUrlSlug(string slug) => BySlug.GetValueOrDefault(slug);

    /// <summary>The on-disk folder that holds a storage part key's question files.</summary>
    public static string FolderForKey(string key) => ByKey.TryGetValue(key, out var part) ? part.Folder : key;

    /// <summary>The display label for a storage part key (e.g. <c>p2b</c> -&gt; <c>II-II</c>).</summary>
    public static string LabelForKey(string key) => ByKey.TryGetValue(key, out var part) ? part.Label : key.ToUpperInvariant();

    /// <summary>The URL slug for a storage part key (e.g. <c>p1</c> -&gt; <c>prima</c>).</summary>
    public static string SlugForKey(string key) => ByKey.TryGetValue(key, out var part) ? part.Slug : key;

    /// <summary>
    /// Translates a storage question id (<c>p1-q002</c>) into its public URL id (<c>prima-q002</c>).
    /// Returns the input unchanged when the part prefix is not recognised.
    /// </summary>
    public static string ToUrlId(string storageId) => MapId(storageId, ByKey, p => p.Slug);

    /// <summary>
    /// Translates a public URL id (<c>prima-q002</c>) back into its storage question id (<c>p1-q002</c>).
    /// Returns the input unchanged when the slug prefix is not recognised.
    /// </summary>
    public static string ToStorageId(string urlId) => MapId(urlId, BySlug, p => p.Key);

    // A URL question id: "<slug>-q<number>", where the number is normally zero-padded to three digits.
    // The number group is captured loosely so hand-typed forms like "prima-q1" can be normalised back to the canonical "prima-q001".
    [GeneratedRegex(@"^(?<slug>[a-z0-9]+)-q(?<number>\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UrlIdRegex();

    /// <summary>
    /// Returns the canonical form of a public URL id, or <c>null</c> when it is already canonical or cannot be canonicalised.
    /// Canonicalisation lower-cases a known slug and zero-pads the question number to three digits, so a hand-typed <c>Prima-q1</c> maps to <c>prima-q001</c>.
    /// Returns <c>null</c> when the slug is unknown or the id is already in canonical form, so callers can cheaply decide whether a redirect is needed.
    /// </summary>
    public static string? Canonicalize(string? urlId)
    {
        if (string.IsNullOrEmpty(urlId))
        {
            return null;
        }

        var match = UrlIdRegex().Match(urlId);
        if (!match.Success || !BySlug.TryGetValue(match.Groups["slug"].Value, out var part))
        {
            return null;
        }

        var number = int.Parse(match.Groups["number"].Value);
        var canonical = $"{part.Slug}-q{number:D3}";
        return string.Equals(canonical, urlId, StringComparison.Ordinal) ? null : canonical;
    }

    private static string MapId(
        string id,
        Dictionary<string, SummaPart> lookup,
        Func<SummaPart, string> pick)
    {
        if (string.IsNullOrEmpty(id))
        {
            return id;
        }

        var separator = id.IndexOf('-');
        var prefix = separator > 0 ? id[..separator] : id;
        if (!lookup.TryGetValue(prefix, out var part))
        {
            return id;
        }

        var remainder = separator > 0 ? id[separator..] : string.Empty;
        return pick(part) + remainder;
    }
}
