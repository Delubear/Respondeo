using Bunit;
using Respondeo.Components.Guides;

namespace Respondeo.UnitTests.Components;

public class GuideTests : TestContext
{
    [Fact]
    public void GuideSteps_renders_an_ordered_list_of_its_children()
    {
        var cut = RenderComponent<GuideSteps>(p => p
            .Add(c => c.ChildContent, b => b.AddMarkupContent(0, "<li>one</li><li>two</li>")));

        var list = cut.Find("ol.guide-steps");
        Assert.Equal(2, list.QuerySelectorAll("li").Length);
    }

    [Fact]
    public void GuideStep_renders_the_plain_cue_and_body()
    {
        var cut = RenderComponent<GuideStep>(p => p
            .Add(c => c.Cue, "Begin")
            .Add(c => c.ChildContent, b => b.AddMarkupContent(0, "<p class=\"body\">do this</p>")));

        Assert.Equal("Begin", cut.Find("li span.guide-cue").TextContent.Trim());
        Assert.NotNull(cut.Find("li p.body"));
    }

    [Fact]
    public void GuideStep_prefers_cue_content_over_the_plain_cue()
    {
        var cut = RenderComponent<GuideStep>(p => p
            .Add(c => c.Cue, "ignored")
            .Add(c => c.CueContent, b => b.AddMarkupContent(0, "Lead <span class=\"mark\">x</span>")));

        var cue = cut.Find("span.guide-cue");
        Assert.DoesNotContain("ignored", cue.TextContent);
        Assert.NotNull(cue.QuerySelector("span.mark"));
    }

    [Fact]
    public void GuideList_renders_an_unordered_list_of_its_children()
    {
        var cut = RenderComponent<GuideList>(p => p
            .Add(c => c.ChildContent, b => b.AddMarkupContent(0, "<li>a</li><li>b</li>")));

        Assert.Equal(2, cut.Find("ul.guide-list").QuerySelectorAll("li").Length);
    }

    [Fact]
    public void GuideItem_renders_the_term_an_em_dash_and_the_description()
    {
        var cut = RenderComponent<GuideItem>(p => p
            .Add(c => c.Term, "Litany")
            .Add(c => c.ChildContent, b => b.AddContent(0, "a call-and-response prayer")));

        var li = cut.Find("li");
        Assert.Equal("Litany", li.QuerySelector("span.guide-term")!.TextContent.Trim());
        Assert.Contains("\u2014", li.TextContent);
        Assert.Contains("a call-and-response prayer", li.TextContent);
    }

    [Fact]
    public void GuideItem_without_a_term_renders_only_its_description()
    {
        var cut = RenderComponent<GuideItem>(p => p
            .Add(c => c.ChildContent, b => b.AddContent(0, "just a description")));

        var li = cut.Find("li");
        Assert.Empty(li.QuerySelectorAll("span.guide-term"));
        Assert.DoesNotContain("\u2014", li.TextContent);
        Assert.Contains("just a description", li.TextContent);
    }

    [Fact]
    public void GuideItem_prefers_term_content_over_the_plain_term()
    {
        var cut = RenderComponent<GuideItem>(p => p
            .Add(c => c.Term, "ignored")
            .Add(c => c.TermContent, b => b.AddMarkupContent(0, "<strong class=\"lead\">The Sentences</strong>"))
            .Add(c => c.ChildContent, b => b.AddContent(0, "description")));

        var li = cut.Find("li");
        Assert.Empty(li.QuerySelectorAll("span.guide-term"));
        Assert.NotNull(li.QuerySelector("strong.lead"));
    }

    [Fact]
    public void GuideLatin_renders_its_content_in_a_latin_span()
    {
        var cut = RenderComponent<GuideLatin>(p => p
            .Add(c => c.ChildContent, b => b.AddContent(0, "versiculus")));

        Assert.Equal("versiculus", cut.Find("span.guide-latin").TextContent.Trim());
    }
}
