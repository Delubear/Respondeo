using System.Text;
using System.Text.RegularExpressions;

namespace Respondeo.Bible;

/// <summary>A resolved passage reference such as <c>John 3:16</c>.</summary>
public sealed record BibleReference(BibleBook Book, int Chapter, int? Verse, int? EndVerse)
{
    /// <summary>A display label using the Douay-Rheims book name.</summary>
    public string Label => Verse is null
        ? $"{Book.Name} {Chapter}"
        : EndVerse is null ? $"{Book.Name} {Chapter}:{Verse}" : $"{Book.Name} {Chapter}:{Verse}-{EndVerse}";

    /// <summary>The app-relative route to the chapter, with the verse range in the query string.</summary>
    public string Href => Verse is null
        ? $"bible/{Book.Slug}/{Chapter}"
        : EndVerse is null ? $"bible/{Book.Slug}/{Chapter}?v={Verse}" : $"bible/{Book.Slug}/{Chapter}?v={Verse}&to={EndVerse}";
}

/// <summary>Parses free-text passage references (e.g. <c>Jn 3:16</c>, <c>1 Cor 13</c>, <c>Ps 22</c>) against the Douay-Rheims canon.</summary>
public static class BibleReferenceParser
{
    // Extra abbreviations keyed by slug. Full names, modern names and slugs are added automatically.
    private static readonly Dictionary<string, string[]> Abbreviations = new()
    {
        ["genesis"] = ["gen", "gn", "ge"],
        ["exodus"] = ["ex", "exod", "exo"],
        ["leviticus"] = ["lev", "lv"],
        ["numbers"] = ["num", "nm", "nb"],
        ["deuteronomy"] = ["deut", "dt", "deu"],
        ["josue"] = ["jos", "josh"],
        ["judges"] = ["judg", "jdg", "jgs"],
        ["ruth"] = ["ru", "rth"],
        ["1-kings"] = ["1kgs", "1sam", "1sm", "1samuel"],
        ["2-kings"] = ["2kgs", "2sam", "2sm", "2samuel"],
        ["3-kings"] = ["3kgs"],
        ["4-kings"] = ["4kgs"],
        ["1-paralipomenon"] = ["1par", "1chr", "1chron"],
        ["2-paralipomenon"] = ["2par", "2chr", "2chron"],
        ["1-esdras"] = ["1esd", "ezr"],
        ["2-esdras"] = ["2esd", "neh"],
        ["tobias"] = ["tob", "tb"],
        ["judith"] = ["jdt", "jud"],
        ["esther"] = ["est", "esth"],
        ["job"] = ["jb"],
        ["psalms"] = ["ps", "psa", "psalm", "pss"],
        ["proverbs"] = ["prov", "prv", "pr"],
        ["ecclesiastes"] = ["eccl", "eccles", "qoh", "ec"],
        ["canticle-of-canticles"] = ["cant", "song", "songofsolomon", "sg"],
        ["wisdom"] = ["wis", "ws"],
        ["ecclesiasticus"] = ["sir", "ecclus"],
        ["isaie"] = ["is", "isa"],
        ["jeremie"] = ["jer", "jr"],
        ["lamentations"] = ["lam", "la"],
        ["baruch"] = ["bar", "ba"],
        ["ezechiel"] = ["ez", "ezek", "ezec"],
        ["daniel"] = ["dan", "dn"],
        ["osee"] = ["os", "hos"],
        ["joel"] = ["jl"],
        ["amos"] = ["am"],
        ["abdias"] = ["abd", "obad", "ob"],
        ["jonas"] = ["jon"],
        ["micheas"] = ["mic", "mi"],
        ["nahum"] = ["na", "nah"],
        ["habacuc"] = ["hab", "hb"],
        ["sophonias"] = ["soph", "zeph", "zep"],
        ["aggeus"] = ["agg", "hag", "hg"],
        ["zacharias"] = ["zach", "zech", "zec"],
        ["malachie"] = ["mal"],
        ["1-machabees"] = ["1mac", "1macc", "1mc"],
        ["2-machabees"] = ["2mac", "2macc", "2mc"],
        ["matthew"] = ["mt", "matt", "mat"],
        ["mark"] = ["mk", "mar", "mrk"],
        ["luke"] = ["lk", "luk"],
        ["john"] = ["jn", "joh", "jhn"],
        ["acts"] = ["ac", "act"],
        ["romans"] = ["rom", "rm", "ro"],
        ["1-corinthians"] = ["1cor", "1co"],
        ["2-corinthians"] = ["2cor", "2co"],
        ["galatians"] = ["gal", "ga"],
        ["ephesians"] = ["eph"],
        ["philippians"] = ["phil", "php"],
        ["colossians"] = ["col"],
        ["1-thessalonians"] = ["1thess", "1thes", "1th"],
        ["2-thessalonians"] = ["2thess", "2thes", "2th"],
        ["1-timothy"] = ["1tim", "1tm"],
        ["2-timothy"] = ["2tim", "2tm"],
        ["titus"] = ["tit", "ti"],
        ["philemon"] = ["phlm", "philem", "phm"],
        ["hebrews"] = ["heb"],
        ["james"] = ["jas", "jm"],
        ["1-peter"] = ["1pet", "1pt"],
        ["2-peter"] = ["2pet", "2pt"],
        ["1-john"] = ["1jn", "1jo"],
        ["2-john"] = ["2jn", "2jo"],
        ["3-john"] = ["3jn", "3jo"],
        ["jude"] = ["jude", "jd"],
        ["apocalypse"] = ["apoc", "ap", "rev", "rv", "revelations"],
    };

    // The Douay-Rheims meaning wins when a key is shared (e.g. "1kings" = 1 Samuel), because Douay names are registered first.
    private static readonly Dictionary<string, BibleBook> Lookup = BuildLookup();

    /// <summary>Parses <paramref name="input"/> as a chapter or verse reference; returns <c>null</c> for anything else, including a bare book name.</summary>
    public static BibleReference? Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var match = ReferencePattern.Match(input.Trim());
        if (!match.Success) return null;

        var key = NormalizeBook(match.Groups["prefix"].Value, match.Groups["name"].Value);
        if (!Lookup.TryGetValue(key, out var book)) return null;

        var chapter = int.Parse(match.Groups["chapter"].Value);
        if (chapter < 1 || chapter > book.ChapterCount) return null;

        int? verse = match.Groups["verse"].Success ? int.Parse(match.Groups["verse"].Value) : null;
        int? end = match.Groups["end"].Success ? int.Parse(match.Groups["end"].Value) : null;
        if (verse is < 1) return null;
        if (end is not null && (verse is null || end <= verse)) end = null;

        return new BibleReference(book, chapter, verse, end);
    }

    private static string NormalizeBook(string prefix, string name)
    {
        var number = prefix.Trim().ToLowerInvariant().TrimEnd('.') switch
        {
            "" => "",
            "1" or "i" or "1st" or "first" => "1",
            "2" or "ii" or "2nd" or "second" => "2",
            "3" or "iii" or "3rd" or "third" => "3",
            "4" or "iv" or "4th" or "fourth" => "4",
            _ => "?",
        };
        return number + Squash(name);
    }

    private static string Squash(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    private static Dictionary<string, BibleBook> BuildLookup()
    {
        var lookup = new Dictionary<string, BibleBook>(StringComparer.Ordinal);
        foreach (var book in BibleBooks.All)
        {
            lookup.TryAdd(Squash(book.Name), book);
            lookup.TryAdd(Squash(book.Slug), book);
        }

        foreach (var book in BibleBooks.All)
        {
            if (book.ModernName is not null) lookup.TryAdd(Squash(book.ModernName), book);
            if (Abbreviations.TryGetValue(book.Slug, out var abbreviations))
            {
                foreach (var abbreviation in abbreviations) lookup.TryAdd(abbreviation, book);
            }
        }

        return lookup;
    }

    private static readonly Regex ReferencePattern = new(
        @"^(?<prefix>(?:[1-4]|1st|2nd|3rd|4th)\.?\s*|(?:i{1,3}|iv|first|second|third|fourth)(?:\.\s*|\s+))?" +
        @"(?<name>[a-z][a-z.\s]*?)\.?\s*(?<chapter>\d{1,3})(?:\s*[:.,]\s*(?<verse>\d{1,3})(?:\s*[-\u2013]\s*(?<end>\d{1,3}))?)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
}
