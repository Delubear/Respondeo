using System.Net.Http.Json;
using System.Text;
using Respondeo.Bible.Contracts;

namespace Respondeo.Bible;

/// <summary>
/// Loads public-domain Bible text from the bundled per-book JSON files: the Douay-Rheims (CC0 dataset by janvier-s)
/// and the Catholic Public Domain Version (yoarikso/cpdvbible), which share the same schema and book slugs.
/// </summary>
internal sealed class DouayRheimsBibleText(HttpClient http) : IBibleText
{
    internal const string TranslationName = "Douay-Rheims";

    private const string DataRoot = "_content/Respondeo.Bible";

    private readonly Dictionary<string, Task<BookFile?>> _books = [];

    private readonly Dictionary<string, Task<Manifest?>> _manifests = [];

    public async Task<IReadOnlyList<BibleVerse>?> GetChapterAsync(string bookSlug, int chapter, BibleTranslation translation = BibleTranslation.DouayRheims)
    {
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
        var book = known is null ? null : await LoadBookAsync(known.Slug, BibleTranslation.DouayRheims);
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

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetAbbreviationsAsync(BibleTranslation translation)
    {
        var manifest = await LoadManifestAsync(translation);
        return (manifest?.Books ?? []).ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<string>)(kv.Value.Abbreviations ?? []),
            StringComparer.Ordinal);
    }

    private async Task<BookFile?> LoadBookAsync(string slug, BibleTranslation translation)
    {
        var manifest = await LoadManifestAsync(translation);
        if (manifest?.Books is null || !manifest.Books.TryGetValue(slug, out var entry) || string.IsNullOrEmpty(entry.File))
        {
            return null;
        }

        var path = $"{DataRoot}/{translation.Folder()}/{entry.File}";
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

    private async Task<Manifest?> LoadManifestAsync(BibleTranslation translation)
    {
        var path = $"{DataRoot}/{translation.Folder()}/manifest.json";
        try
        {
            if (!_manifests.TryGetValue(path, out var load))
            {
                load = http.GetFromJsonAsync<Manifest>(path);
                _manifests[path] = load;
            }

            return await load;
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException or NotSupportedException)
        {
            _manifests.Remove(path);
            return null;
        }
    }

    private sealed record Manifest(Dictionary<string, ManifestEntry>? Books);

    private sealed record ManifestEntry(string? File, string? Name, string[]? Abbreviations);

    private sealed record BookFile(List<ChapterFile>? Chapters);

    private sealed record ChapterFile(int Chapter, List<VerseFile>? Verses);

    private sealed record VerseFile(int Verse, string? Text);
}
