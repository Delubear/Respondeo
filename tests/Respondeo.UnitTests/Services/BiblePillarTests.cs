using Respondeo.Bible;
using Respondeo.Bible.Contracts;

namespace Respondeo.UnitTests.Services;

public class BiblePillarTests
{
    [Theory]
    [InlineData("bible", SitePillars.BibleId)]
    [InlineData("bible/drb/genesis/1", SitePillars.BibleId)]
    [InlineData("summa", SitePillars.SummaId)]
    [InlineData("discover/prayers", SitePillars.DiscoverId)]
    [InlineData("", SitePillars.JourneyId)]
    [InlineData("biblestudy", SitePillars.JourneyId)]
    public void ResolveActiveId_MatchesPillar(string path, string expected) => Assert.Equal(expected, SitePillars.ResolveActiveId(path));

    [Fact]
    public void Items_IncludesBible() => Assert.Contains(SitePillars.Items, p => p.Id == SitePillars.BibleId && p.Href == "bible");

    [Fact]
    public void DrbManifest_HasCatholicCanon()
    {
        var drb = Respondeo.UnitTests.TestSupport.TestBible.Drb;
        var books = drb.Books();
        Assert.Equal(73, books.Count(b => drb.Testament(b) != BibleTestament.Appendix));
        Assert.Equal(3, books.Count(b => drb.Testament(b) == BibleTestament.Appendix));
        Assert.Equal(46, books.Count(b => drb.Testament(b) == BibleTestament.Old));
        Assert.Equal("genesis", books[0].Slug);
    }

    [Fact]
    public void FindBook_IsCaseInsensitive_AndNullForUnknown()
    {
        var drb = Respondeo.UnitTests.TestSupport.TestBible.Drb;
        Assert.Equal("Isaias", drb.BookName(drb.FindBook("ISAIAS")!));
        Assert.Null(drb.FindBook("nope"));
    }
}
