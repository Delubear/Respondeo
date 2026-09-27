using Respondeo.SummaImporter;

namespace Respondeo.UnitTests.Importer;

/// <summary>
/// Unit tests for <see cref="SummaParser.FormatParagraph"/>, which tokenises the classic Summa section
/// cues (Objection, On the contrary, I answer that, Reply to Objection) at the start of a paragraph into
/// neutral <c>{{scue|...}}</c> markers. Regression coverage for the respondeo cue, whose source form
/// occasionally omits the comma after "I answer that" and previously leaked into the preceding section.
/// </summary>
public class FormatParagraphTests
{
    [Fact]
    public void Tokenises_respondeo_with_a_comma()
    {
        var result = SummaParser.FormatParagraph("I answer that, it is so.");

        Assert.StartsWith("{{scue|respondeo}}", result);
        Assert.DoesNotContain("I answer that", result);
    }

    [Fact]
    public void Tokenises_respondeo_without_a_comma()
    {
        var result = SummaParser.FormatParagraph("I answer that As stated above, a martyr is a witness.");

        Assert.StartsWith("{{scue|respondeo}}", result);
        Assert.DoesNotContain("I answer that", result);
        Assert.Contains("As stated above", result);
    }

    [Fact]
    public void Tokenises_contra_cue()
    {
        var result = SummaParser.FormatParagraph("On the contrary, Augustine says otherwise.");

        Assert.StartsWith("{{scue|contra}}", result);
    }

    [Theory]
    [InlineData("Objection 1: It seems not.", "{{scue|objection|1}}")]
    [InlineData("Objection 2. Further, it seems not.", "{{scue|objection|2}}")]
    [InlineData("Reply to Objection 1: The first fails.", "{{scue|reply|1}}")]
    public void Tokenises_numbered_cues(string text, string expectedToken)
    {
        var result = SummaParser.FormatParagraph(text);

        Assert.StartsWith(expectedToken, result);
    }

    [Fact]
    public void Leaves_ordinary_prose_untouched()
    {
        const string text = "This is an ordinary paragraph with no leading cue.";

        var result = SummaParser.FormatParagraph(text);

        Assert.Equal(text, result);
    }
}
