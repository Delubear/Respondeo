using Respondeo.Bible.Contracts;

namespace Respondeo.UnitTests.Bible;

public class BibleChapterNavigationTests
{
    private static readonly BibleTranslation Translation = BibleTranslation.Cpdv;

    [Fact]
    public void Previous_FirstBookFirstChapter_IsNull()
    {
        var first = BibleBooks.All.First(b => Translation.Books().Contains(b));
        Assert.Null(BibleChapterNavigation.Previous(first, 1, Translation));
    }

    [Fact]
    public void Next_WithinBook_GoesToFollowingChapter()
    {
        var first = BibleBooks.All.First(b => Translation.Books().Contains(b));
        var next = BibleChapterNavigation.Next(first, 1, Translation);
        Assert.Equal($"bible/{first.Slug}/2", next?.Href);
    }

    [Fact]
    public void Next_LastChapter_CrossesToNextBook()
    {
        var books = BibleBooks.All.Where(b => Translation.Books().Contains(b)).ToList();
        var last = Translation.ChapterCount(books[0]);
        var next = BibleChapterNavigation.Next(books[0], last, Translation);
        Assert.Equal($"bible/{books[1].Slug}/1", next?.Href);
    }

    [Fact]
    public void Previous_FirstChapter_CrossesToPriorBookLastChapter()
    {
        var books = BibleBooks.All.Where(b => Translation.Books().Contains(b)).ToList();
        var previous = BibleChapterNavigation.Previous(books[1], 1, Translation);
        Assert.Equal($"bible/{books[0].Slug}/{Translation.ChapterCount(books[0])}", previous?.Href);
    }
}
