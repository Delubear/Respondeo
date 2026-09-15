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
    public void Always_renders_the_articles_link()
    {
        var cut = RenderComponent<Breadcrumb>();

        var articles = cut.Find("a.breadcrumb__articles");
        Assert.Equal("Articles", articles.TextContent);
        Assert.Equal("articles", articles.GetAttribute("href"));
    }

    [Fact]
    public void Renders_current_title_as_the_final_crumb()
    {
        var cut = RenderComponent<Breadcrumb>(p => p
            .Add(c => c.CurrentTitle, "Does God exist?"));

        var current = cut.Find(".breadcrumb__current");
        Assert.Equal("Does God exist?", current.TextContent);
        Assert.Equal("Does God exist?", current.GetAttribute("title"));
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

        // One separator before the Articles link, one before each ancestor crumb, plus one before the current node.
        Assert.Equal(crumbs.Length + 2, cut.FindAll(".breadcrumb__sep").Count);
    }

    [Fact]
    public void Renders_no_ancestor_links_when_trail_is_empty()
    {
        var cut = RenderComponent<Breadcrumb>(p => p.Add(c => c.CurrentTitle, "Home"));

        Assert.Empty(cut.FindAll("a.breadcrumb__link"));
        // Separators before the Articles link and before the current node.
        Assert.Equal(2, cut.FindAll(".breadcrumb__sep").Count);
    }
}
