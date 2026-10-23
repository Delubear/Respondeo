using Respondeo.Bible;
using Respondeo.Bible.Contracts;

namespace Respondeo.UnitTests.Services;

public class BibleCitationTests
{
    [Fact]
    public void Psalm_UsesVulgateNumbering()
    {
        var passage = BibleCitation.Parse("Psalm 63:2-9");

        Assert.NotNull(passage);
        Assert.Equal("psalms", passage.BookSlug);
        Assert.Equal(new VerseSpan(62, 2, 62, 9), Assert.Single(passage.Spans));
    }

    [Fact]
    public void CommaList_AndVerseSuffixes_AreParsed()
    {
        var passage = BibleCitation.Parse("Exodus 15:1-4a, 8-13, 17-18");

        Assert.NotNull(passage);
        Assert.Equal(
            [new VerseSpan(15, 1, 15, 4), new VerseSpan(15, 8, 15, 13), new VerseSpan(15, 17, 15, 18)],
            passage.Spans);
    }

    [Fact]
    public void CrossChapter_AndSemicolons_AreParsed()
    {
        Assert.Equal(new VerseSpan(61, 10, 62, 5), Assert.Single(BibleCitation.Parse("Isaiah 61:10-62:5")!.Spans));

        var rev = BibleCitation.Parse("Revelation 4:11; 5:9, 10, 12");
        Assert.Equal("apocalypse", rev!.BookSlug);
        Assert.Equal(new VerseSpan(5, 12, 5, 12), rev.Spans[^1]);
    }

    [Theory]
    [InlineData("Psalm 9:2-11", 9, 2, 11)]
    [InlineData("Psalm 10:1-4", 9, 22, 25)]
    [InlineData("Psalm 114:1-8", 113, 1, 8)]
    [InlineData("Psalm 115:1-3", 113, 9, 11)]
    [InlineData("Psalm 116:10-19", 115, 1, 10)]
    [InlineData("Psalm 147:12-20", 147, 1, 9)]
    public void Psalm_SplitAndMerged_MapToDouay(string citation, int chapter, int start, int end) =>
        Assert.Equal(new VerseSpan(chapter, start, chapter, end), Assert.Single(BibleCitation.Parse(citation)!.Spans));

    [Fact]
    public void Unknown_ReturnsNull() => Assert.Null(BibleCitation.Parse("not a citation"));
}
