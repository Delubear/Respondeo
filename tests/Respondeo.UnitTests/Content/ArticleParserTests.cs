using Respondeo.Content.Articles;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Content;

/// <summary>
/// Unit tests for <see cref="ArticleParser"/>, which maps a raw article Markdown file (YAML front-matter plus
/// body) into the typed domain document. Covers source-label dash normalization so authors can type a plain
/// hyphen in numeric ranges rather than a "&ndash;" entity.
/// </summary>
public class ArticleParserTests
{
    private static ArticleParser CreateParser() => new(ContentRendering.Renderer);

    [Theory]
    [InlineData("Catechism of the Catholic Church, 1285-1321", "Catechism of the Catholic Church, 1285\u20131321")]
    [InlineData("Acts of the Apostles 8:14-17; 19:5-6", "Acts of the Apostles 8:14\u201317; 19:5\u20136")]
    [InlineData("Pre-Congregation saints", "Pre-Congregation saints")]
    public void Parse_converts_plain_hyphen_number_ranges_in_source_labels_to_en_dash(string input, string expected)
    {
        var raw = $"""
            ---
            id: dashed-article
            title: "A Dashed Article"
            sources:
              - label: "{input}"
                url: "https://example.com"
            ---

            ## Section

            Body text.
            """;

        var doc = CreateParser().Parse(raw);

        Assert.NotNull(doc);
        Assert.Equal(expected, doc!.Sources[0].Label);
    }
}
