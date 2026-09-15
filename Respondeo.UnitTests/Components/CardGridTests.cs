using Bunit;
using Microsoft.AspNetCore.Components;
using Respondeo.Components;
using Xunit;

namespace Respondeo.UnitTests.Components;

public class CardGridTests : TestContext
{
    [Fact]
    public void Wraps_child_content_in_the_grid_container()
    {
        var cut = RenderComponent<CardGrid>(p => p.AddChildContent("<div class=\"child\">Hello</div>"));

        var grid = cut.Find("div.card-grid");
        var child = grid.QuerySelector("div.child");
        Assert.NotNull(child);
        Assert.Equal("Hello", child!.TextContent);
    }

    [Fact]
    public void Renders_empty_grid_when_no_children_provided()
    {
        var cut = RenderComponent<CardGrid>();

        var grid = cut.Find("div.card-grid");
        Assert.Empty(grid.Children);
    }
}
