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

    [Fact]
    public void Splits_trailing_arrow_into_its_own_span()
    {
        var cut = RenderComponent<Card>(p => p
            .Add(c => c.Href, "/node/home")
            .Add(c => c.Title, "Home")
            .Add(c => c.Cta, "Read \u2192"));

        // The arrow is isolated so it can animate independently; the label is trimmed and the gap
        // is restored visually by CSS margin, so the DOM text has no literal space before the arrow.
        Assert.Equal("Read\u2192", cut.Find("span.card__cta").TextContent);
        Assert.Equal("\u2192", cut.Find("span.card__cta-arrow").TextContent);
    }

    [Fact]
    public void Omits_visited_badge_by_default()
    {
        var cut = RenderComponent<Card>(p => p
            .Add(c => c.Href, "/node/home")
            .Add(c => c.Title, "Home"));

        Assert.Empty(cut.FindAll("span.card__check"));
        Assert.Empty(cut.FindAll("a.card--visited"));
    }

    [Fact]
    public void Renders_visited_badge_when_visited()
    {
        var cut = RenderComponent<Card>(p => p
            .Add(c => c.Href, "/node/home")
            .Add(c => c.Title, "Home")
            .Add(c => c.Status, CardStatus.Visited));

        Assert.NotNull(cut.Find("span.card__check"));
        Assert.NotNull(cut.Find("a.card--visited"));
    }

    [Fact]
    public void Renders_in_progress_ribbon_when_in_progress()
    {
        var cut = RenderComponent<Card>(p => p
            .Add(c => c.Href, "/node/home")
            .Add(c => c.Title, "Home")
            .Add(c => c.Status, CardStatus.InProgress));

        Assert.NotNull(cut.Find("span.card__ribbon"));
        Assert.NotNull(cut.Find("a.card--wip"));
    }

    [Fact]
    public void In_progress_status_does_not_also_render_visited_check()
    {
        var cut = RenderComponent<Card>(p => p
            .Add(c => c.Href, "/node/home")
            .Add(c => c.Title, "Home")
            .Add(c => c.Status, CardStatus.InProgress));

        // A card advertises exactly one corner treatment, so the ribbon and the check never co-exist.
        Assert.Empty(cut.FindAll("span.card__check"));
    }

    [Fact]
    public void Feast_glow_can_coexist_with_an_in_progress_ribbon()
    {
        var cut = RenderComponent<Card>(p => p
            .Add(c => c.Href, "/node/home")
            .Add(c => c.Title, "Home")
            .Add(c => c.Status, CardStatus.InProgress)
            .Add(c => c.Feast, true));

        // Feast is a full-card glow, not a corner badge, so it is orthogonal to the status badge.
        Assert.NotNull(cut.Find("a.card--feast"));
        Assert.NotNull(cut.Find("a.card--wip"));
        Assert.NotNull(cut.Find("span.card__ribbon"));
    }

    [Fact]
    public void Renders_all_pills()
    {
        var cut = RenderComponent<Card>(p => p
            .Add(c => c.Href, "/node/home")
            .Add(c => c.Title, "Home")
            .Add(c => c.Pills, new[] { "First", "Second" }));

        var pills = cut.FindAll("span.card__pill").ToList();
        Assert.Equal(2, pills.Count);
        Assert.Equal("First", pills[0].TextContent);
        Assert.Equal("Second", pills[1].TextContent);
    }
}
