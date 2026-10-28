using Respondeo.Bible.Contracts;
using Respondeo.Content.Contracts;
using Respondeo.Content.Summa.Contracts;

namespace Respondeo.Services.Navigation;

/// <summary>
/// Loads the data a destination page needs before the router swaps to it, so the current page stays on screen until the next one
/// can render completely from cache (no Loading placeholder, no half-built layout).
/// Invoked from the Router's <c>OnNavigateAsync</c>, which the router awaits before rendering the new route.
/// </summary>
public sealed class RoutePrefetcher(
    IContentService content,
    IArticleService articles,
    ISaintService saints,
    IMiracleService miracles,
    IPrayerService prayers,
    ISummaService summa,
    ISummaPartCatalog parts,
    IBibleTranslations bibleTranslations,
    IBibleChapterReader bibleReader)
{
    public async Task PrefetchAsync(string path)
    {
        var segments = path.Split('?', '#')[0].Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        try
        {
            switch (segments)
            {
                case ["summa", var id]:
                    await Task.WhenAll(summa.GetQuestionAsync(parts.ToStorageId(parts.Canonicalize(id) ?? id)), summa.GetIndexAsync());
                    break;
                case ["bible", var code, var book]:
                    await PrefetchBibleChapterAsync(code, book, 1);
                    break;
                case ["bible", var code, var book, var chapter] when int.TryParse(chapter, out var number):
                    await PrefetchBibleChapterAsync(code, book, number);
                    break;
                case ["discover", "articles", var id]:
                    await Task.WhenAll(articles.GetArticleAsync(id), prayers.GetIndexAsync());
                    break;
                case ["discover", "saints", var id]:
                    await Task.WhenAll(saints.GetFacetsAsync(), saints.GetByIdAsync(id), prayers.GetIndexAsync());
                    break;
                case ["discover", "miracles", var id]:
                    await Task.WhenAll(miracles.GetFacetsAsync(), miracles.GetByIdAsync(id), prayers.GetIndexAsync());
                    break;
                case ["discover", "prayers", var id]:
                    await prayers.GetPrayerAsync(id);
                    break;
                case [_, var id]:
                    await content.GetByIdAsync(id);
                    break;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // A failed prefetch just means the page loads its own data (and shows its own error/not-found state).
        }
    }

    private async Task PrefetchBibleChapterAsync(string code, string book, int chapter)
    {
        if (await bibleTranslations.FindAsync(code) is not { } translation) return;
        await bibleReader.LoadAsync(translation.FindBook(book), chapter, translation);
    }
}
