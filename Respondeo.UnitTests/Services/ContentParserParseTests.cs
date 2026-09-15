using Respondeo.Content.Markdown.Services;

namespace Respondeo.UnitTests.Services;

public class ContentParserParseTests
{
    [Fact]
    public void Parses_sections_list_in_order()
    {
        var raw = "---\nid: parent\ntitle: Parent\nsections:\n  - one\n  - two\n  - three\n---\nBody";

        var node = new ContentParser().Parse(raw);

        Assert.NotNull(node);
        Assert.Equal(["one", "two", "three"], node!.Sections);
    }

    [Fact]
    public void Sections_defaults_to_empty_when_absent()
    {
        var raw = "---\nid: parent\ntitle: Parent\n---\nBody";

        var node = new ContentParser().Parse(raw);

        Assert.NotNull(node);
        Assert.Empty(node!.Sections);
    }
}
