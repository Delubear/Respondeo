using Bunit;
using Respondeo.Components;

namespace Respondeo.UnitTests.Components;

public class FilterLayoutTests : TestContext
{
    [Fact]
    public void Renders_rail_and_child_content_in_the_grid()
    {
        var cut = RenderComponent<FilterLayout>(p => p
            .Add(c => c.Rail, b => b.AddMarkupContent(0, "<div class=\"my-filters\">filters</div>"))
            .Add(c => c.ChildContent, b => b.AddMarkupContent(0, "<div class=\"my-results\">results</div>")));

        var layout = cut.Find("div.filter-layout");
        Assert.NotNull(layout.QuerySelector("aside.filter-rail .my-filters"));
        Assert.NotNull(layout.QuerySelector(".my-results"));
    }

    [Fact]
    public void Omits_the_search_box_when_no_search_is_supplied()
    {
        var cut = RenderComponent<FilterLayout>(p => p
            .Add(c => c.Rail, b => b.AddMarkupContent(0, "<div>filters</div>"))
            .Add(c => c.ChildContent, b => b.AddMarkupContent(0, "<div>results</div>")));

        Assert.Empty(cut.FindAll(".my-search"));
    }

    [Fact]
    public void Renders_the_search_slot_above_the_grid_when_supplied()
    {
        var cut = RenderComponent<FilterLayout>(p => p
            .Add(c => c.Search, b => b.AddMarkupContent(0, "<div class=\"my-search\">search</div>"))
            .Add(c => c.Rail, b => b.AddMarkupContent(0, "<div>filters</div>"))
            .Add(c => c.ChildContent, b => b.AddMarkupContent(0, "<div>results</div>")));

        // Search is rendered before the grid in document order.
        var markup = cut.Markup;
        Assert.Contains("my-search", markup);
        Assert.True(
            markup.IndexOf("my-search", StringComparison.Ordinal) < markup.IndexOf("filter-layout", StringComparison.Ordinal),
            "Expected the search slot to precede the filter-layout grid.");
    }
}
