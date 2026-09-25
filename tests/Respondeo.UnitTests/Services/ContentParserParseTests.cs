using Respondeo.Content.Markdown.Services;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Services;

public class ContentParserParseTests
{
    [Fact]
    public void Parses_sections_list_in_order()
    {
        var raw = "---\nid: parent\ntitle: Parent\nsections:\n  - one\n  - two\n  - three\n---\nBody";

        var node = new ContentParser(ContentRendering.Renderer).Parse(raw);

        Assert.NotNull(node);
        Assert.Equal(["one", "two", "three"], node!.Sections);
    }

    [Fact]
    public void Sections_defaults_to_empty_when_absent()
    {
        var raw = "---\nid: parent\ntitle: Parent\n---\nBody";

        var node = new ContentParser(ContentRendering.Renderer).Parse(raw);

        Assert.NotNull(node);
        Assert.Empty(node!.Sections);
    }

    [Fact]
    public void Parses_topics_list_in_order()
    {
        var raw = "---\nid: parent\ntitle: Parent\ntopics:\n  - Existence of God\n  - St. Thomas Aquinas\n---\nBody";

        var node = new ContentParser(ContentRendering.Renderer).Parse(raw);

        Assert.NotNull(node);
        Assert.Equal(["Existence of God", "St. Thomas Aquinas"], node!.Topics);
    }

    [Fact]
    public void Topics_defaults_to_empty_when_absent()
    {
        var raw = "---\nid: parent\ntitle: Parent\n---\nBody";

        var node = new ContentParser(ContentRendering.Renderer).Parse(raw);

        Assert.NotNull(node);
        Assert.Empty(node!.Topics);
    }

    [Fact]
    public void Stage_is_taken_from_the_supplied_value()
    {
        var raw = "---\nid: aquinas\ntitle: Aquinas\n---\nBody";

        var node = new ContentParser(ContentRendering.Renderer).Parse(raw, "why-god");

        Assert.NotNull(node);
        Assert.Equal("why-god", node!.Stage);
    }

    [Fact]
    public void Stage_defaults_to_null_when_not_supplied()
    {
        var raw = "---\nid: parent\ntitle: Parent\n---\nBody";

        var node = new ContentParser(ContentRendering.Renderer).Parse(raw);

        Assert.NotNull(node);
        Assert.Null(node!.Stage);
    }
}
