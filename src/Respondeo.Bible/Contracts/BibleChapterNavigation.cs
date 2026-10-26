namespace Respondeo.Bible.Contracts;

/// <summary>A link to a Bible chapter page.</summary>
public sealed record ChapterLink(string Href, string Label);

/// <summary>Computes previous/next chapter links within a translation, crossing book boundaries.</summary>
public static class BibleChapterNavigation
{
    public static ChapterLink? Previous(BibleBook book, int chapter, BibleTranslation translation)
    {
        var count = translation.ChapterCount(book);
        if (chapter > 1 && count > 0)
        {
            var target = Math.Min(chapter - 1, count);
            return Link(book, target, translation);
        }

        var books = translation.Books();
        var prior = BibleBooks.All.Take(IndexOf(book)).LastOrDefault(books.Contains);
        return prior is null ? null : Link(prior, translation.ChapterCount(prior), translation);
    }

    public static ChapterLink? Next(BibleBook book, int chapter, BibleTranslation translation)
    {
        if (chapter < translation.ChapterCount(book)) return Link(book, chapter + 1, translation);
        var books = translation.Books();
        var following = BibleBooks.All.Skip(IndexOf(book) + 1).FirstOrDefault(books.Contains);
        return following is null ? null : Link(following, 1, translation);
    }

    private static ChapterLink Link(BibleBook book, int chapter, BibleTranslation translation) =>
        new($"bible/{book.Slug}/{chapter}", $"{translation.BookName(book)} {chapter}");

    private static int IndexOf(BibleBook book)
    {
        for (var i = 0; i < BibleBooks.All.Count; i++)
        {
            if (BibleBooks.All[i] == book) return i;
        }

        return -1;
    }
}
