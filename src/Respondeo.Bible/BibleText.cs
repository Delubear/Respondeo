using System.Net.Http.Json;
using System.Text;

namespace Respondeo.Bible;

/// <summary>Loads Bible text for any configured translation from its bundled per-book JSON files.</summary>
internal sealed class BibleText(HttpClient http, IBibleTranslations translations) : IBibleText
{
    private readonly Dictionary<string, Task<BookFile?>> _books = [];

    public async Task<IReadOnlyList<BibleVerse>?> GetChapterAsync(string bookSlug, int chapter, BibleTranslation? translation = null)
    {
        translation ??= await translations.GetDefaultAsync();
        var book = await LoadBookAsync(bookSlug, translation);
        var match = book?.Chapters?.FirstOrDefault(c => c.Chapter == chapter);
        if (match?.Verses is null)
        {
            return null;
        }

        return [.. match.Verses
            .Select(v => new BibleVerse(v.Verse, CleanVerse(bookSlug, chapter, v.Verse, v.Text)))
            .Where(v => v.Text.Length > 0)];
    }

    public async Task<string?> GetPassageAsync(string citation)
    {
        var translation = await translations.GetDefaultAsync();
        var passage = BibleCitation.Parse(citation, translation);
        if (passage is null)
        {
            return null;
        }

        var book = await LoadBookAsync(passage.BookSlug, translation);
        if (book?.Chapters is null)
        {
            return null;
        }

        var text = new StringBuilder();
        foreach (var span in passage.Spans)
        {
            foreach (var chapter in book.Chapters.Where(c => c.Chapter >= span.Chapter && c.Chapter <= span.EndChapter))
            {
                foreach (var verse in chapter.Verses ?? [])
                {
                    var afterStart = chapter.Chapter > span.Chapter || verse.Verse >= span.StartVerse;
                    var beforeEnd = chapter.Chapter < span.EndChapter || verse.Verse <= span.EndVerse;
                    var cleaned = CleanVerse(passage.BookSlug, chapter.Chapter, verse.Verse, verse.Text);
                    if (afterStart && beforeEnd && cleaned.Length > 0)
                    {
                        text.AppendLine(cleaned);
                    }
                }
            }
        }

        return text.Length == 0 ? null : text.ToString().TrimEnd();
    }

    private static string CleanVerse(string bookSlug, int chapter, int verse, string? text)
    {
        var trimmed = text?.Trim() ?? string.Empty;
        return bookSlug == "psalms" ? CleanPsalmVerse(chapter, verse, trimmed) : trimmed;
    }

    /// <summary>Psalms whose leading verses are entirely superscription in the Douay-Rheims numbering.</summary>
    private static readonly Dictionary<int, int> PsalmTitleVerseCount = new()
    {
        [33] = 1, [50] = 2, [51] = 2, [53] = 2, [59] = 2, [71] = 1,
    };

    /// <summary>Removes a psalm's superscription from the given verse, returning an empty string when the verse is wholly title.</summary>
    internal static string CleanPsalmVerse(int chapter, int verse, string text)
    {
        if (PsalmTitleVerseCount.TryGetValue(chapter, out var count) && verse <= count)
        {
            return string.Empty;
        }

        if (verse != 1)
        {
            return text;
        }

        var marker = chapter switch
        {
            17 => "and he said:",
            96 => "restored again to him.",
            _ => null,
        };
        if (marker is not null)
        {
            var index = text.IndexOf(marker, StringComparison.Ordinal);
            return index < 0 ? text : text[(index + marker.Length)..].TrimStart();
        }

        return StripPsalmTitle(text);
    }

    /// <summary>Removes leading psalm superscriptions (e.g. "Unto the end, the Psalm of David."), which liturgical books omit.</summary>
    internal static string StripPsalmTitle(string text)
    {
        while (true)
        {
            var match = PsalmTitle.Match(text);
            if (!match.Success)
            {
                return text;
            }

            text = text[match.Length..].TrimStart();
        }
    }

    private static readonly System.Text.RegularExpressions.Regex PsalmTitle = new(
        @"^[^.:;?!]{0,160}?\b(psalm|canticle|unto the end|inscription|david himself|asaph|sons of core|understanding|alleluia|prayer of|degrees)\b[^.:;?!]{0,160}?\.(\s+|$)",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

    private async Task<BookFile?> LoadBookAsync(string slug, BibleTranslation translation)
    {
        if (!translation.Manifest.TryGetValue(slug, out var entry) || string.IsNullOrEmpty(entry.File))
        {
            return null;
        }

        var path = $"{BibleTranslationCatalog.DataRoot}/{translation.Folder}/{entry.File}";
        try
        {
            if (!_books.TryGetValue(path, out var load))
            {
                load = http.GetFromJsonAsync<BookFile>(path);
                _books[path] = load;
            }

            return await load;
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException or NotSupportedException)
        {
            _books.Remove(path);
            return null;
        }
    }

    private sealed record BookFile(List<ChapterFile>? Chapters);

    private sealed record ChapterFile(int Chapter, List<VerseFile>? Verses);

    private sealed record VerseFile(int Verse, string? Text);
}
