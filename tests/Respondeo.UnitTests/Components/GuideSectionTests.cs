using Bunit;
using Respondeo.Components.Guides;

namespace Respondeo.UnitTests.Components;

public class GuideSectionTests : TestContext
{
    [Fact]
    public void Renders_the_plain_title_in_the_subhead()
    {
        var cut = RenderComponent<GuideSection>(p => p
            .Add(c => c.Title, "How to pray a litany")
            .Add(c => c.ChildContent, b => b.AddMarkupContent(0, "<p>body</p>")));

        Assert.Equal("How to pray a litany", cut.Find("h3.guide-subhead").TextContent.Trim());
    }

    [Fact]
    public void Renders_the_body_content()
    {
        var cut = RenderComponent<GuideSection>(p => p
            .Add(c => c.Title, "Title")
            .Add(c => c.ChildContent, b => b.AddMarkupContent(0, "<p class=\"body\">body</p>")));

        Assert.NotNull(cut.Find("p.body"));
    }

    [Fact]
    public void Prefers_title_content_over_plain_title()
    {
        var cut = RenderComponent<GuideSection>(p => p
            .Add(c => c.Title, "ignored")
            .Add(c => c.TitleContent, b => b.AddMarkupContent(0, "Versicle <span class=\"mark\">V.</span>")));

        var subhead = cut.Find("h3.guide-subhead");
        Assert.DoesNotContain("ignored", subhead.TextContent);
        Assert.NotNull(subhead.QuerySelector("span.mark"));
    }
}
