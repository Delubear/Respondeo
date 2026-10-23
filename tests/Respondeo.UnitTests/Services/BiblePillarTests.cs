using Respondeo.Bible;
using Respondeo.Bible.Contracts;

namespace Respondeo.UnitTests.Services;

public class BiblePillarTests
{
    [Theory]
    [InlineData("bible", SitePillars.BibleId)]
    [InlineData("bible/genesis/1", SitePillars.BibleId)]
    [InlineData("summa", SitePillars.SummaId)]
    [InlineData("discover/prayers", SitePillars.DiscoverId)]
    [InlineData("", SitePillars.JourneyId)]
    [InlineData("biblestudy", SitePillars.JourneyId)]
    public void ResolveActiveId_MatchesPillar(string path, string expected) => Assert.Equal(expected, SitePillars.ResolveActiveId(path));

    [Fact]
    public void Items_IncludesBible() => Assert.Contains(SitePillars.Items, p => p.Id == SitePillars.BibleId && p.Href == "bible");

    [Fact]
    public void BibleBooks_HasCatholicCanon()
    {
        Assert.Equal(73, BibleBooks.All.Count(b => b.Testament != BibleTestament.Appendix));
        Assert.Equal(3, BibleBooks.All.Count(b => b.Testament == BibleTestament.Appendix));
        Assert.Equal(46, BibleBooks.All.Count(b => b.Testament == BibleTestament.Old));
        Assert.Equal(BibleBooks.All.Count, BibleBooks.All.Select(b => b.Slug).Distinct().Count());
    }

    [Fact]
    public void Find_IsCaseInsensitive_AndNullForUnknown()
    {
        Assert.Equal("Isaias", BibleBooks.Find("ISAIE")?.Name);
        Assert.Null(BibleBooks.Find("nope"));
    }
}
