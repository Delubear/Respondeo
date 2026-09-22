using Bunit;
using Respondeo.Components;

namespace Respondeo.UnitTests.Components;

public class BreadcrumbTests : TestContext
{
    [Fact]
    public void Always_renders_the_home_link()
    {
        var cut = RenderComponent<Breadcrumb>();

        var home = cut.Find("nav.breadcrumb a");
        Assert.Equal("Home", home.TextContent);
        Assert.Equal("", home.GetAttribute("href"));
    }

    [Fact]
    public void Renders_the_articles_link_only_when_show_articles_is_set()
    {
        var cut = RenderComponent<Breadcrumb>(p => p
            .Add(c => c.ShowArticles, true));

        var articles = cut.Find("a.breadcrumb__articles");
        Assert.Equal("Articles", articles.TextContent);
        Assert.Equal("articles", articles.GetAttribute("href"));
    }

    [Fact]
    public void Omits_the_articles_link_by_default()
    {
        var cut = RenderComponent<Breadcrumb>();

        Assert.Empty(cut.FindAll("a.breadcrumb__articles"));
    }

    [Fact]
    public void Renders_current_title_as_the_final_crumb()
    {
        var cut = RenderComponent<Breadcrumb>(p => p.Add(c => c.CurrentTitle, "Does God exist?"));

        var current = cut.Find(".breadcrumb__current");
        Assert.Equal("Does God exist?", current.TextContent);
        Assert.Equal("Does God exist?", current.GetAttribute("title"));
    }

    [Fact]
    public void Marks_the_current_crumb_with_aria_current()
    {
        var cut = RenderComponent<Breadcrumb>(p => p.Add(c => c.CurrentTitle, "Does God exist?"));

        Assert.Equal("page", cut.Find(".breadcrumb__current").GetAttribute("aria-current"));
    }

    [Fact]
    public void Exposes_a_breadcrumb_navigation_label()
    {
        var cut = RenderComponent<Breadcrumb>();

        Assert.Equal("Breadcrumb", cut.Find("nav.breadcrumb").GetAttribute("aria-label"));
    }

    [Fact]
    public void Shows_ellipsis_placeholder_when_current_title_is_null()
    {
        var cut = RenderComponent<Breadcrumb>();

        Assert.Equal("…", cut.Find(".breadcrumb__current").TextContent);
    }

    [Fact]
    public void Renders_a_link_for_each_ancestor_crumb()
    {
        var crumbs = new[]
        {
            new Breadcrumb.Crumb("atheist", "Atheist"),
            new Breadcrumb.Crumb("aquinas-five-ways", "Does God exist?"),
        };

        var cut = RenderComponent<Breadcrumb>(p => p
            .Add(c => c.Crumbs, crumbs)
            .Add(c => c.CurrentTitle, "First Way: Motion"));

        var links = cut.FindAll("a.breadcrumb__link").ToList();
        Assert.Equal(2, links.Count);

        Assert.Equal("Atheist", links[0].TextContent);
        Assert.Equal("node/atheist", links[0].GetAttribute("href"));
        Assert.Equal("Atheist", links[0].GetAttribute("title"));

        Assert.Equal("Does God exist?", links[1].TextContent);
        Assert.Equal("node/aquinas-five-ways", links[1].GetAttribute("href"));
    }

    [Fact]
    public void Renders_a_separator_for_each_crumb_and_the_current_node()
    {
        var crumbs = new[]
        {
            new Breadcrumb.Crumb("atheist", "Atheist"),
            new Breadcrumb.Crumb("aquinas-five-ways", "Does God exist?"),
        };

        var cut = RenderComponent<Breadcrumb>(p => p
            .Add(c => c.Crumbs, crumbs)
            .Add(c => c.CurrentTitle, "First Way: Motion"));

        // One separator before each ancestor crumb, plus one before the current node.
        Assert.Equal(crumbs.Length + 1, cut.FindAll(".breadcrumb__sep").Count);
    }

    [Fact]
    public void Renders_no_ancestor_links_when_trail_is_empty()
    {
        var cut = RenderComponent<Breadcrumb>(p => p.Add(c => c.CurrentTitle, "Home"));

        Assert.Empty(cut.FindAll("a.breadcrumb__link"));
        // Only the separator before the current node.
        Assert.Single(cut.FindAll(".breadcrumb__sep"));
    }

    [Fact]
    public void Roots_the_breadcrumb_at_the_stage_when_stage_href_is_set()
    {
        var cut = RenderComponent<Breadcrumb>(p => p
            .Add(c => c.StageHref, "why-god")
            .Add(c => c.StageLabel, "Why God?"));

        var root = cut.Find("nav.breadcrumb a");
        Assert.Equal("Why God?", root.TextContent);
        Assert.Equal("why-god", root.GetAttribute("href"));
    }
}
