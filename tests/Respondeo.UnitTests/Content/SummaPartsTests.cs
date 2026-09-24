using Respondeo.Content.Summa;

namespace Respondeo.UnitTests.Content;

public class SummaPartsTests
{
    [Theory]
    [InlineData("prima-q1", "prima-q001")]
    [InlineData("prima-q02", "prima-q002")]
    [InlineData("secsec-q7", "secsec-q007")]
    [InlineData("Prima-Q1", "prima-q001")]
    [InlineData("suppl-q99", "suppl-q099")]
    public void Canonicalize_pads_and_lowercases_known_ids(string input, string expected)
    {
        Assert.Equal(expected, SummaParts.Canonicalize(input));
    }

    [Theory]
    [InlineData("prima-q001")]
    [InlineData("secsec-q189")]
    [InlineData("suppl-q101")]
    public void Canonicalize_returns_null_when_already_canonical(string input)
    {
        Assert.Null(SummaParts.Canonicalize(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("unknown-q1")]
    [InlineData("prima")]
    [InlineData("prima-a1")]
    public void Canonicalize_returns_null_for_unrecognized_or_malformed_ids(string? input)
    {
        Assert.Null(SummaParts.Canonicalize(input));
    }
}
