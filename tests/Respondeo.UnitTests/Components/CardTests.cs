using Bunit;
using Respondeo.Components;

namespace Respondeo.UnitTests.Components;

public class CardTests : TestContext
{
    [Fact]
    public void Renders_h2_by_default()
    {
        var cut = RenderComponent<Card>(p => p
            .Add(c => c.Href, "/node/home")
            .Add(c => c.Title, "Home"));

        var heading = cut.Find("h2.card__title");
        Assert.Equal("Home", heading.TextContent);
        Assert.Equal("/node/home", cut.Find("a.card").GetAttribute("href"));
    }

    [Fact]
    public void Renders_h3_when_level_is_three()
    {
        var cut = RenderComponent<Card>(p => p
            .Add(c => c.Href, "/node/branch")
            .Add(c => c.Title, "Branch")
            .Add(c => c.Level, 3));

        Assert.NotNull(cut.Find("h3.card__title"));
        Assert.Empty(cut.FindAll("h2.card__title"));
    }

    [Fact]
    public void Omits_summary_when_not_provided()
    {
        var cut = RenderComponent<Card>(p => p
            .Add(c => c.Href, "/node/home")
            .Add(c => c.Title, "Home"));

        Assert.Empty(cut.FindAll("p.card__summary"));
    }

    [Fact]
    public void Renders_summary_when_provided()
    {
        var cut = RenderComponent<Card>(p => p
            .Add(c => c.Href, "/node/home")
            .Add(c => c.Title, "Home")
            .Add(c => c.Summary, "A short summary"));

        Assert.Equal("A short summary", cut.Find("p.card__summary").TextContent);
    }

    [Fact]
    public void Uses_custom_cta_text()
    {
        var cut = RenderComponent<Card>(p => p
            .Add(c => c.Href, "/node/home")
            .Add(c => c.Title, "Home")
            .Add(c => c.Cta, "Start here"));

        Assert.Equal("Start here", cut.Find("span.card__cta").TextContent);
    }
}
