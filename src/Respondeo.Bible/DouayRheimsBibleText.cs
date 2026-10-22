using System.Net.Http.Json;
using System.Text;

namespace Respondeo.Bible;

/// <summary>Loads public-domain Douay-Rheims text (CC0 dataset by janvier-s) from the bundled per-book JSON files.</summary>
internal sealed class DouayRheimsBibleText(HttpClient http) : IBibleText
{
    internal const string TranslationName = "Douay-Rheims";

    private const string DataPath = "_content/Respondeo.Bible/douay-rheims";

    private readonly Dictionary<string, Task<BookFile?>> _books = [];

    public async Task<string?> GetPassageAsync(string citation)
    {
        var passage = BibleCitation.Parse(citation);
        if (passage is null)
        {
            return null;
        }

        BookFile? book;
        try
        {
            if (!_books.TryGetValue(passage.BookSlug, out var load))
            {
                load = http.GetFromJsonAsync<BookFile>($"{DataPath}/{passage.BookSlug}.json");
                _books[passage.BookSlug] = load;
            }

            book = await load;
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException or NotSupportedException)
        {
            _books.Remove(passage.BookSlug);
            return null;
        }

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

    private sealed record BookFile(List<ChapterFile>? Chapters);

    private sealed record ChapterFile(int Chapter, List<VerseFile>? Verses);

    private sealed record VerseFile(int Verse, string? Text);
}
