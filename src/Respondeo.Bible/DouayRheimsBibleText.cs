using System.Net.Http.Json;
using System.Text;
using Respondeo.Bible.Contracts;

namespace Respondeo.Bible;

/// <summary>
/// Loads public-domain Bible text from the bundled per-book JSON files: the Douay-Rheims (CC0 dataset by janvier-s)
/// and the Catholic Public Domain Version (yoarikso/cpdvbible), which share the same schema and book slugs.
/// </summary>
internal sealed class DouayRheimsBibleText(HttpClient http, IBibleTranslations translations) : IBibleText
{
    private readonly Dictionary<string, Task<BookFile?>> _books = [];

    public async Task<IReadOnlyList<BibleVerse>?> GetChapterAsync(string bookSlug, int chapter, BibleTranslation? translation = null)
    {
        translation ??= await translations.GetDefaultAsync();
        var known = BibleBooks.Find(bookSlug);
        if (known is null)
        {
            return null;
        }

        var book = await LoadBookAsync(known.Slug, translation);
        var match = book?.Chapters?.FirstOrDefault(c => c.Chapter == chapter);
        if (match?.Verses is null)
        {
            return null;
        }

        return [.. match.Verses
            .Where(v => !string.IsNullOrWhiteSpace(v.Text))
            .Select(v => new BibleVerse(v.Verse, v.Text!.Trim()))];
    }

    public async Task<string?> GetPassageAsync(string citation)
    {
        var passage = BibleCitation.Parse(citation);
        if (passage is null)
        {
            return null;
        }

        var known = BibleBooks.Find(passage.BookSlug);
        var book = known is null ? null : await LoadBookAsync(known.Slug, await translations.GetDefaultAsync());
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
                    if (afterStart && beforeEnd && !string.IsNullOrWhiteSpace(verse.Text))
                    {
                        text.AppendLine(verse.Text.Trim());
                    }
                }
            }
        }

        return text.Length == 0 ? null : text.ToString().TrimEnd();
    }

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
