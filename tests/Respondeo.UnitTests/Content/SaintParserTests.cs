using Respondeo.Content.Saints;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Content;

/// <summary>
/// Unit tests for <see cref="SaintParser"/>, which maps a raw saint Markdown file (YAML front-matter plus
/// body) into the typed domain document. Covers slug normalization, list de-duplication, patronage casing,
/// unvetted detection, and front-matter guard behavior.
/// </summary>
public class SaintParserTests
{
    private static SaintParser CreateParser() => new(ContentRendering.Renderer);

    [Fact]
    public void Parse_maps_front_matter_and_renders_body()
    {
        const string raw = """
            ---
            id: john-of-the-cross
            title: "St. John of the Cross"
            summary: Mystic and poet.
            era: Early-Modern
            region: Europe
            patronages:
              - Mystics
              - mystics
            statesOfLife: [Religious, PRIEST, religious]
            canonizations: [Canonized]
            dates: "1542–1591"
            reviewStatus: unvetted
            ---

            ## Life

            The dark night of the soul.
            """;

        var doc = CreateParser().Parse(raw);

        Assert.NotNull(doc);
        Assert.Equal("john-of-the-cross", doc!.Id);
        // Slugs are trimmed and lowercased.
        Assert.Equal("early-modern", doc.Era);
        Assert.Equal("europe", doc.Region);
        // Lists de-duplicate case-insensitively and lowercase slugs.
        Assert.Equal(["religious", "priest"], doc.StatesOfLife);
        Assert.Equal(["canonized"], doc.Canonizations);
        // Patronages keep author casing but drop case-insensitive duplicates.
        Assert.Equal(["Mystics"], doc.Patronages);
        Assert.True(doc.IsUnvetted);
        Assert.Contains("dark night", doc.BodyHtml);
    }

    [Fact]
    public void Parse_falls_back_to_unknown_for_blank_facets()
    {
        const string raw = """
            ---
            id: anonymous-martyr
            title: "An Anonymous Martyr"
            ---

            A witness.
            """;

        var doc = CreateParser().Parse(raw);

        Assert.NotNull(doc);
        Assert.Equal("unknown", doc!.Era);
        Assert.Equal("unknown", doc.Region);
        Assert.Empty(doc.StatesOfLife);
        Assert.Empty(doc.Canonizations);
        Assert.False(doc.IsUnvetted);
    }

    [Fact]
    public void Parse_returns_null_when_front_matter_is_absent()
    {
        var doc = CreateParser().Parse("Just a body with no front matter.");

        Assert.Null(doc);
    }

    [Fact]
    public void Parse_returns_null_when_front_matter_is_malformed_yaml()
    {
        const string raw = """
            ---
            id: broken
            patronages:
              - Mystics
             - badly-indented
            ---

            ## Life
            """;

        var doc = CreateParser().Parse(raw);

        Assert.Null(doc);
    }

    [Theory]
    [InlineData("1225-1274", "1225\u20131274")]
    [InlineData("1225 - 1274", "1225\u20131274")]
    [InlineData("1181/82-1226", "1181/82\u20131226")]
    public void Parse_converts_plain_hyphen_date_ranges_to_en_dash(string input, string expected)
    {
        var raw = $"""
            ---
            id: dated-saint
            title: "A Dated Saint"
            dates: "{input}"
            ---

            A life.
            """;

        var doc = CreateParser().Parse(raw);

        Assert.NotNull(doc);
        Assert.Equal(expected, doc!.Dates);
    }
}
