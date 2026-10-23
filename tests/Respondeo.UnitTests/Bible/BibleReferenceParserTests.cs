using Respondeo.Bible;
using Respondeo.Bible.Contracts;

namespace Respondeo.UnitTests.Bible;

public class BibleReferenceParserTests
{
    [Theory]
    [InlineData("John 3", "john", 3, null, null)]
    [InlineData("Jn 3:16", "john", 3, 16, null)]
    [InlineData("jn3.16", "john", 3, 16, null)]
    [InlineData("1 Cor 13:4-7", "1-corinthians", 13, 4, 7)]
    [InlineData("I Cor 13", "1-corinthians", 13, null, null)]
    [InlineData("1Jn 4:8", "1-john", 4, 8, null)]
    [InlineData("Ps 22", "psalms", 22, null, null)]
    [InlineData("Isaiah 53", "isaie", 53, null, null)]
    [InlineData("Isaias 53:5", "isaie", 53, 5, null)]
    [InlineData("Rev 21", "apocalypse", 21, null, null)]
    [InlineData("Sirach 2", "ecclesiasticus", 2, null, null)]
    [InlineData("1 Kings 3", "1-kings", 3, null, null)]
    [InlineData("1 Samuel 3", "1-kings", 3, null, null)]
    [InlineData("Song of Songs 2", "canticle-of-canticles", 2, null, null)]
    public void Parses_references(string input, string slug, int chapter, int? verse, int? end)
    {
        var reference = BibleReferenceParser.Parse(input);

        Assert.NotNull(reference);
        Assert.Equal(slug, reference.Book.Slug);
        Assert.Equal(chapter, reference.Chapter);
        Assert.Equal(verse, reference.Verse);
        Assert.Equal(end, reference.EndVerse);
    }

    [Theory]
    [InlineData("")]
    [InlineData("John")]
    [InlineData("John 22")]
    [InlineData("Nothing 3")]
    [InlineData("love one another")]
    public void Rejects_non_references(string input) => Assert.Null(BibleReferenceParser.Parse(input));

    [Fact]
    public void Builds_verse_href() =>
        Assert.Equal("bible/john/3?v=16", BibleReferenceParser.Parse("Jn 3:16")!.Href);
}
