using Respondeo.Bible;
using Respondeo.Bible.Contracts;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Bible;

public class BibleReferenceParserTests
{
    private static BibleReference? Parse(string input, string translation = "drb") =>
        BibleReferenceParser.Parse(input, TestBible.Get(translation));

    [Theory]
    [InlineData("John 3", "john", 3, null, null)]
    [InlineData("Jn 3:16", "john", 3, 16, null)]
    [InlineData("jn3.16", "john", 3, 16, null)]
    [InlineData("1 Cor 13:4-7", "1-corinthians", 13, 4, 7)]
    [InlineData("I Cor 13", "1-corinthians", 13, null, null)]
    [InlineData("1Jn 4:8", "1-john", 4, 8, null)]
    [InlineData("Ps 22", "psalms", 22, null, null)]
    [InlineData("Isaiah 53", "isaiah", 53, null, null)]
    [InlineData("Isaias 53:5", "isaiah", 53, 5, null)]
    [InlineData("Rev 21", "revelation", 21, null, null)]
    [InlineData("Sirach 2", "sirach", 2, null, null)]
    [InlineData("1 Kings 3", "1-samuel", 3, null, null)]
    [InlineData("1 Samuel 3", "1-samuel", 3, null, null)]
    [InlineData("Song of Songs 2", "song-of-songs", 2, null, null)]
    public void Parses_references(string input, string slug, int chapter, int? verse, int? end)
    {
        var reference = Parse(input);

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
    public void Rejects_non_references(string input) => Assert.Null(Parse(input));

    [Fact]
    public void Builds_verse_href() =>
        Assert.Equal("bible/drb/john/3?v=16", Parse("Jn 3:16")!.Href);

    [Theory]
    [InlineData("1 Kings 1", "drb", "1-samuel", "bible/drb/1-kings/1")]
    [InlineData("1 Kings 1", "cpdv", "1-kings", "bible/cpdv/1-kings/1")]
    [InlineData("1 Kgs 1", "cpdv", "1-kings", "bible/cpdv/1-kings/1")]
    [InlineData("3 Kings 1", "drb", "1-kings", "bible/drb/3-kings/1")]
    [InlineData("1 Samuel 1", "cpdv", "1-samuel", "bible/cpdv/1-samuel/1")]
    [InlineData("1 Samuel 1", "drb", "1-samuel", "bible/drb/1-kings/1")]
    public void Resolves_books_by_translation_naming(string input, string translation, string key, string href)
    {
        var reference = Parse(input, translation)!;
        Assert.Equal(key, reference.Book.Slug);
        Assert.Equal(href, reference.Href);
    }

    [Fact]
    public void Rejects_books_missing_from_translation() =>
        Assert.Null(Parse("Prayer of Manasses 1", "cpdv"));

    [Theory]
    [InlineData("Jn 3:5-7,16", "bible/drb/john/3?v=5-7,16", "John 3:5-7,16")]
    [InlineData("1 Cor 13:4-7", "bible/drb/1-corinthians/13?v=4-7", "1 Corinthians 13:4-7")]
    [InlineData("Jn 3:16, 5", "bible/drb/john/3?v=5,16", "John 3:5,16")]
    public void Builds_multi_range_href(string input, string href, string label)
    {
        var reference = Parse(input)!;
        Assert.Equal(href, reference.Href);
        Assert.Equal(label, reference.Label);
    }
}

public class VerseSelectionTests
{
    [Theory]
    [InlineData("5-7,16", "5-7,16")]
    [InlineData("16,5-7", "5-7,16")]
    [InlineData("7-5", "5-7")]
    [InlineData("5-7,6-9", "5-9")]
    [InlineData("5-7,8", "5-8")]
    [InlineData("5,x,0,-3", "5")]
    [InlineData("", "")]
    public void Parses_selections(string input, string expected) =>
        Assert.Equal(expected, VerseSelection.Parse(input).ToString());

    [Fact]
    public void Contains_checks_every_range()
    {
        var selection = VerseSelection.Parse("5-7,16");
        Assert.True(selection.Contains(6));
        Assert.True(selection.Contains(16));
        Assert.False(selection.Contains(8));
        Assert.Equal(5, selection.First);
    }
}
