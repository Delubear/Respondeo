using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Respondeo.Bible.Contracts;

/// <summary>A resolved passage reference such as <c>John 3:16</c>.</summary>
public sealed record BibleReference(BibleBook Book, int Chapter, int? Verse, int? EndVerse)
{
    /// <summary>The full verse selection; defaults to <see cref="Verse"/>-<see cref="EndVerse"/>.</summary>
    public VerseSelection Verses { get; init; } = Verse is { } v
        ? VerseSelection.FromRanges([(v, EndVerse is { } e && e > v ? e : v)])
        : VerseSelection.Empty;

    /// <summary>A display label using the common modern book name.</summary>
    public string Label => Verses.IsEmpty
        ? $"{Translation.BookName(Book)} {Chapter}"
        : $"{Translation.BookName(Book)} {Chapter}:{Verses}";

    /// <summary>The translation whose book naming the reference was resolved against.</summary>
    public required BibleTranslation Translation { get; init; }

    /// <summary>The app-relative route to the chapter in <see cref="Translation"/>, with the verse selection in the query string.</summary>
    public string Href => Translation.ChapterHref(Book, Chapter, Verses.IsEmpty ? null : Verses.ToString());
}

/// <summary>Parses free-text passage references (e.g. <c>Jn 3:16</c>, <c>1 Cor 13</c>, <c>Ps 22</c>) using the book naming of the selected translation.</summary>
public static class BibleReferenceParser
{
    // Book names collide across translations ("1 Kings" is 1 Samuel in the Douay-Rheims but 3 Kings in modern Bibles),
    // so each translation gets its own lookup in which its own naming wins.
    private static readonly ConditionalWeakTable<BibleTranslation, Dictionary<string, BibleBook>> Lookups = new();

    /// <summary>
    /// Parses <paramref name="input"/> as a chapter or verse reference using the book naming and manifest abbreviations
    /// of <paramref name="translation"/>; returns <c>null</c> for anything else, including a bare book name or a book the translation lacks.
    /// </summary>
    public static BibleReference? Parse(string? input, BibleTranslation translation)
    {
        var lookup = Lookups.GetValue(translation, BuildLookup);

        if (string.IsNullOrWhiteSpace(input)) return null;

        var match = ReferencePattern.Match(input.Trim());
        if (!match.Success) return null;

        var key = NormalizeBook(match.Groups["prefix"].Value, match.Groups["name"].Value);
        if (!lookup.TryGetValue(key, out var book)) return null;

        var chapter = int.Parse(match.Groups["chapter"].Value);
        if (chapter < 1 || chapter > translation.ChapterCount(book)) return null;

        if (!match.Groups["verses"].Success) return new BibleReference(book, chapter, null, null) { Translation = translation };

        var selection = VerseSelection.Parse(match.Groups["verses"].Value);
        if (selection.IsEmpty) return null;

        var first = selection.Ranges[0];
        return new BibleReference(book, chapter, first.Start, first.End > first.Start ? first.End : null)
        {
            Verses = selection,
            Translation = translation,
        };
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

    private static Dictionary<string, BibleBook> BuildLookup(BibleTranslation translation)
    {
        var books = translation.Books();
        var lookup = new Dictionary<string, BibleBook>(StringComparer.Ordinal);

        // The translation's own names and slugs are registered first so they win any collision.
        foreach (var book in books)
        {
            lookup.TryAdd(Squash(translation.BookName(book)), book);
            lookup.TryAdd(Squash(translation.Slug(book)), book);
        }

        // Abbreviations come from the translation's manifest, so their meaning follows its naming tradition.
        foreach (var book in books)
        {
            foreach (var abbreviation in translation.Entry(book)?.Abbreviations ?? []) lookup.TryAdd(Squash(abbreviation), book);
        }

        foreach (var book in books)
        {
            lookup.TryAdd(Squash(book.Name), book);
            lookup.TryAdd(Squash(book.Slug), book);
            lookup.TryAdd(Squash(book.FileSlug), book);
            if (book.ModernName is not null) lookup.TryAdd(Squash(book.ModernName), book);
        }

        return lookup;
    }

    private static readonly Regex ReferencePattern = new(
        @"^(?<prefix>(?:[1-4]|1st|2nd|3rd|4th)\.?\s*|(?:i{1,3}|iv|first|second|third|fourth)(?:\.\s*|\s+))?" +
        @"(?<name>[a-z][a-z.\s]*?)\.?\s*(?<chapter>\d{1,3})" +
        @"(?:\s*[:.,]\s*(?<verses>\d{1,3}(?:\s*[-\u2013]\s*\d{1,3})?(?:\s*,\s*\d{1,3}(?:\s*[-\u2013]\s*\d{1,3})?)*))?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
}
