using System.Text.RegularExpressions;
using Respondeo.Content.Summa.Contracts;

namespace Respondeo.Content.Summa;

/// <summary>
/// The single source of truth mapping a Summa part between its stable storage key, its URL slug, its display label, its title, and its on-disk folder.
/// Storage keys are permanent; the slug and label are presentation concerns that can change without touching the generated corpus.
///
/// Question ids come in two forms that this type translates between:
/// <list type="bullet">
///   <item><c>storage id</c>  - <c>{key}-q{n:D3}</c>, e.g. <c>p1-q002</c> (used in files and tokens).</item>
///   <item><c>url id</c>      - <c>{slug}-q{n:D3}</c>, e.g. <c>prima-q002</c> (used in routes/links).</item>
/// </list>
/// The class is public and stateless so the build-time sitemap tool (which has no DI host) can instantiate it directly;
/// app consumers depend on <see cref="ISummaPartCatalog"/> and receive it through dependency injection.
/// </summary>
public sealed partial class SummaPartCatalog : ISummaPartCatalog
{
    // Reading order. Key = permanent storage id; Slug = URL form; Label = cross-reference display.
    public IReadOnlyList<SummaPart> All { get; } =
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

    private readonly Dictionary<string, SummaPart> _byKey;
    private readonly Dictionary<string, SummaPart> _bySlug;

    public SummaPartCatalog()
    {
        _byKey = All.ToDictionary(p => p.Key, StringComparer.OrdinalIgnoreCase);
        _bySlug = All.ToDictionary(p => p.Slug, StringComparer.OrdinalIgnoreCase);
    }

    public SummaPart? ByStorageKey(string key) => _byKey.GetValueOrDefault(key);

    public SummaPart? ByUrlSlug(string slug) => _bySlug.GetValueOrDefault(slug);

    public string FolderForKey(string key) => _byKey.TryGetValue(key, out var part) ? part.Folder : key;

    public string LabelForKey(string key) => _byKey.TryGetValue(key, out var part) ? part.Label : key.ToUpperInvariant();

    public string SlugForKey(string key) => _byKey.TryGetValue(key, out var part) ? part.Slug : key;

    public string ToUrlId(string storageId) => MapId(storageId, _byKey, p => p.Slug);

    public string ToStorageId(string urlId) => MapId(urlId, _bySlug, p => p.Key);

    // A URL question id: "<slug>-q<number>", where the number is normally zero-padded to three digits.
    // The number group is captured loosely so hand-typed forms like "prima-q1" can be normalised back to the canonical "prima-q001".
    [GeneratedRegex(@"^(?<slug>[a-z0-9]+)-q(?<number>\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UrlIdRegex();

    public string? Canonicalize(string? urlId)
    {
        if (string.IsNullOrEmpty(urlId))
        {
            return null;
        }

        var match = UrlIdRegex().Match(urlId);
        if (!match.Success || !_bySlug.TryGetValue(match.Groups["slug"].Value, out var part))
        {
            return null;
        }

        var number = int.Parse(match.Groups["number"].Value);
        var canonical = $"{part.Slug}-q{number:D3}";
        return string.Equals(canonical, urlId, StringComparison.Ordinal) ? null : canonical;
    }

    private static string MapId(string id, Dictionary<string, SummaPart> lookup, Func<SummaPart, string> pick)
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
