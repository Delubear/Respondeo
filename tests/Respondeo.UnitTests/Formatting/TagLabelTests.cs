using Respondeo;

namespace Respondeo.UnitTests.Formatting;

/// <summary>
/// Verifies tag slugs are title-cased consistently: every word is capitalized except minor joining
/// words, and the first word is always capitalized regardless.
/// </summary>
public class TagLabelTests
{
    [Theory]
    [InlineData("prayer", "Prayer")]
    [InlineData("confession", "Confession")]
    [InlineData("faith-and-reason", "Faith and Reason")]
    [InlineData("existence of god", "Existence of God")]
    [InlineData("why-god", "Why God")]
    [InlineData("jesus christ", "Jesus Christ")]
    [InlineData("the-church", "The Church")]
    [InlineData("of-the-eucharist", "Of the Eucharist")]
    public void Formats_tags_in_title_case(string tag, string expected)
    {
        Assert.Equal(expected, TagLabel.Format(tag));
    }

    [Fact]
    public void Leading_minor_word_is_still_capitalized()
    {
        Assert.Equal("And Reason", TagLabel.Format("and-reason"));
    }

    [Fact]
    public void Collapses_repeated_separators()
    {
        Assert.Equal("Faith and Reason", TagLabel.Format("faith--and  reason"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Returns_input_unchanged_when_empty(string? tag)
    {
        Assert.Equal(tag, TagLabel.Format(tag!));
    }
}
