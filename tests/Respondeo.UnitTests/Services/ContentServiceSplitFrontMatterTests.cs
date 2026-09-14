using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class ContentServiceSplitFrontMatterTests
{
    [Fact]
    public void Returns_null_frontmatter_when_no_delimiter()
    {
        var (fm, body) = ContentService.SplitFrontMatter("# Just a heading\n\nText");

        Assert.Null(fm);
        Assert.Equal("# Just a heading\n\nText", body);
    }

    [Fact]
    public void Splits_frontmatter_and_body()
    {
        var raw = "---\nid: home\ntitle: Home\n---\n# Body\n\nHello";

        var (fm, body) = ContentService.SplitFrontMatter(raw);

        Assert.NotNull(fm);
        Assert.Contains("id: home", fm);
        Assert.Contains("title: Home", fm);
        Assert.Equal("# Body\n\nHello", body);
    }

    [Fact]
    public void Handles_crlf_line_endings()
    {
        var raw = "---\r\nid: node\r\n---\r\nBody text";

        var (fm, body) = ContentService.SplitFrontMatter(raw);

        Assert.NotNull(fm);
        Assert.Contains("id: node", fm);
        Assert.Equal("Body text", body);
    }

    [Fact]
    public void Handles_leading_bom_and_whitespace()
    {
        var raw = "\uFEFF\n---\nid: node\n---\nBody";

        var (fm, body) = ContentService.SplitFrontMatter(raw);

        Assert.NotNull(fm);
        Assert.Contains("id: node", fm);
        Assert.Equal("Body", body);
    }

    [Fact]
    public void Returns_null_when_frontmatter_not_terminated()
    {
        var raw = "---\nid: node\nno closing delimiter";

        var (fm, _) = ContentService.SplitFrontMatter(raw);

        Assert.Null(fm);
    }
}
