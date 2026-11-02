using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests;

public class BreviaryTextTests
{
    [Fact]
    public void Tokenize_SplitsAllBreviaryMarks()
    {
        var segments = BreviaryText.Tokenize("\u2123 \u2720 Lord, \u2020\nopen * my lips.");

        Assert.Equal(
            [
                BreviarySymbol.Versicle, BreviarySymbol.Text, BreviarySymbol.Cross, BreviarySymbol.Text,
                BreviarySymbol.Flexa, BreviarySymbol.Text, BreviarySymbol.Mediant, BreviarySymbol.Text,
            ],
            segments.Select(s => s.Kind));
        Assert.Equal("\u2123 \u2720 Lord, \u2020\nopen * my lips.", string.Concat(segments.Select(s => s.Text)));
    }

    [Fact]
    public void Tokenize_PlainTextIsOneSegment()
    {
        var segment = Assert.Single(BreviaryText.Tokenize("Amen."));
        Assert.Equal(BreviarySymbol.Text, segment.Kind);
    }

    [Fact]
    public void Tokenize_EmptyReturnsNothing() => Assert.Empty(BreviaryText.Tokenize(""));

    [Fact]
    public void ExpandTokens_ReplacesEveryToken() =>
        Assert.Equal("\u2123 \u2720 Lord, \u2020\nopen * my lips.\n\u211F Amen.", BreviaryText.ExpandTokens("{{V}} {{cross}} Lord, {{dagger}}\nopen {{star}} my lips.\n{{R}} Amen."));

    [Fact]
    public void Tokenize_ExpandsTokens()
    {
        var segments = BreviaryText.Tokenize("{{V}} {{cross}} Lord");
        Assert.Equal([BreviarySymbol.Versicle, BreviarySymbol.Text, BreviarySymbol.Cross, BreviarySymbol.Text], segments.Select(s => s.Kind));
    }

    [Theory]
    [InlineData("src/Respondeo.LiturgyOfTheHours/Data")]
    [InlineData("src/Respondeo.LiturgyOfTheHours/SampleContent")]
    public void ContentJson_UsesTokensRatherThanRawGlyphs(string relativeDirectory)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Respondeo.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var files = Directory.GetFiles(Path.Combine(dir!.FullName, relativeDirectory), "*.json", SearchOption.AllDirectories);
        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.All(BreviaryText.TokenOnlyGlyphs, glyph => Assert.False(text.Contains(glyph), $"{Path.GetFileName(file)} contains raw '{glyph}'; use its {{{{token}}}} instead."));
        }
    }
}
