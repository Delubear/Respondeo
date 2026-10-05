using Respondeo.SummaImporter;

namespace Respondeo.UnitTests.Importer;

/// <summary>
/// Unit tests for the Summa importer title helpers: <see cref="SummaParser.NormalizeTitle"/> (whitespace
/// collapse, stray CCEL bracket-reference removal, orphan-asterisk cleanup) and
/// <see cref="SummaParser.TitleCase"/> (ALL-CAPS to title case with lowercase function words).
/// </summary>
public class TitleHelpersTests
{
    [Fact]
    public void NormalizeTitle_collapses_whitespace()
    {
        Assert.Equal("The Existence of God", SummaParser.NormalizeTitle("  The   Existence of\tGod  "));
    }

    [Fact]
    public void NormalizeTitle_strips_bracket_cross_references()
    {
        Assert.Equal("Of the Angels", SummaParser.NormalizeTitle("Of the Angels [76]"));
    }

    [Fact]
    public void NormalizeTitle_removes_orphan_asterisks()
    {
        Assert.Equal("Of Fear", SummaParser.NormalizeTitle("Of Fear*"));
    }

    [Fact]
    public void NormalizeTitle_keeps_asterisk_opening_an_inline_gloss()
    {
        Assert.Equal("[*Scientia]", SummaParser.NormalizeTitle("[*Scientia]"));
    }

    [Theory]
    [InlineData("THE EXISTENCE OF GOD", "The Existence of God")]
    [InlineData("OF THE SIMPLICITY OF GOD", "Of the Simplicity of God")]
    [InlineData("GOD IN RELATION TO CREATURES", "God in Relation to Creatures")]
    public void TitleCase_capitalizes_words_but_lowercases_function_words(string upper, string expected)
    {
        Assert.Equal(expected, SummaParser.TitleCase(upper));
    }

    [Fact]
    public void TitleCase_capitalizes_a_leading_function_word()
    {
        Assert.Equal("The Angels", SummaParser.TitleCase("THE ANGELS"));
    }
}
