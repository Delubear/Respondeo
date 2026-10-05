using Respondeo.Content.Prayers;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Content;

public class PrayerParserTests
{
    private static readonly PrayerParser Parser = new(ContentRendering.Renderer);

    [Fact]
    public void Renders_versicle_and_response_markers_as_literal_text()
    {
        const string raw = """
            ---
            id: a-versicle
            title: "A Versicle"
            summary: A versicle and response.
            language: en
            ---

            V. O God, come to my assistance.
            R. O Lord, make haste to help me.
            """;

        var prayer = Parser.Parse(raw);

        Assert.NotNull(prayer);
        // Markdig would otherwise treat "V."/"R." as ordered-list markers and drop them; the parser
        // escapes the dot so the markers survive as literal text.
        Assert.Contains("V.", prayer!.Html);
        Assert.Contains("R.", prayer.Html);
        Assert.DoesNotContain("<ol", prayer.Html);
    }

    [Fact]
    public void Leaves_ordinary_prayers_untouched()
    {
        const string raw = """
            ---
            id: hail-mary
            title: "Hail Mary"
            summary: The angelic salutation.
            language: en
            ---

            Hail Mary, full of grace, the Lord is with thee.
            """;

        var prayer = Parser.Parse(raw);

        Assert.NotNull(prayer);
        Assert.Contains("full of grace", prayer!.Html);
        Assert.DoesNotContain("\\", prayer.Html);
    }
}
