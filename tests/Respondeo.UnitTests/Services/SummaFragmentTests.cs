using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class SummaFragmentTests
{
    [Theory]
    [InlineData("article-3", 3)]
    [InlineData("article-12", 12)]
    [InlineData("article-3-reply-2", 3)]
    [InlineData("article-3-objection-1", 3)]
    [InlineData("article-3-contra", 3)]
    public void TryGetArticleNumber_ParsesOwningArticle(string fragment, int expected)
    {
        var parsed = SummaFragment.TryGetArticleNumber(fragment, out var articleNumber);

        Assert.True(parsed);
        Assert.Equal(expected, articleNumber);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("prologue")]
    [InlineData("article-")]
    [InlineData("article-abc")]
    public void TryGetArticleNumber_RejectsNonArticleFragments(string? fragment)
    {
        var parsed = SummaFragment.TryGetArticleNumber(fragment, out var articleNumber);

        Assert.False(parsed);
        Assert.Equal(0, articleNumber);
    }

    [Fact]
    public void ArticleAnchor_BuildsBareAnchor()
    {
        Assert.Equal("article-5", SummaFragment.ArticleAnchor(5));
    }
}
