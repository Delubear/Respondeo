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
}
