namespace Respondeo.Bible;

internal sealed class BibleChapterReader(IBibleText bible, IBibleTranslations translations) : IBibleChapterReader
{
    public async Task<BibleChapterResult> LoadAsync(BibleBook? book, int chapter, BibleTranslation translation)
    {
        if (book is null || chapter < 1) return BibleChapterResult.NotFound;

        var verses = await bible.GetChapterAsync(book.Slug, chapter, translation);
        if (verses is not null) return new BibleChapterResult(verses, false);

        foreach (var other in await translations.GetAllAsync())
        {
            if (other != translation && await bible.GetChapterAsync(book.Slug, chapter, other) is not null)
            {
                return new BibleChapterResult(null, true);
            }
        }

        return BibleChapterResult.NotFound;
    }
}
