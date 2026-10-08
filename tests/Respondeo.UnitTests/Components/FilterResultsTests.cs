using Bunit;
using Respondeo.Components;

namespace Respondeo.UnitTests.Components;

public class FilterResultsTests : TestContext
{
    [Fact]
    public void Renders_the_count_line()
    {
        var cut = RenderComponent<FilterResults>(p => p
            .Add(c => c.FilteredCount, 3)
            .Add(c => c.TotalCount, 12)
            .Add(c => c.Noun, "saints")
            .Add(c => c.EmptyMessage, "none")
            .Add(c => c.ChildContent, b => b.AddMarkupContent(0, "<div>card</div>")));

        Assert.Equal("3 of 12 saints", cut.Find("p.results-count").TextContent.Trim());
    }

    [Fact]
    public void Shows_the_empty_state_when_nothing_matches()
    {
        var cut = RenderComponent<FilterResults>(p => p
            .Add(c => c.FilteredCount, 0)
            .Add(c => c.TotalCount, 9)
            .Add(c => c.Noun, "saints")
            .Add(c => c.EmptyMessage, "No saints match your filters."));

        Assert.Equal("No saints match your filters.", cut.Find("p.empty-state").TextContent.Trim());
        Assert.Empty(cut.FindAll("div.card-grid"));
    }

    [Fact]
    public void Shows_a_single_column_grid_of_children_when_results_match()
    {
        var cut = RenderComponent<FilterResults>(p => p
            .Add(c => c.FilteredCount, 2)
            .Add(c => c.TotalCount, 2)
            .Add(c => c.Noun, "articles")
            .Add(c => c.EmptyMessage, "none")
            .Add(c => c.ChildContent, b => b.AddMarkupContent(0, "<article class=\"item\">A</article><article class=\"item\">B</article>")));

        Assert.Empty(cut.FindAll("p.empty-state"));
        var grid = cut.Find("div.card-grid.card-grid--single");
        Assert.Equal(2, grid.QuerySelectorAll("article.item").Length);
    }

    [Fact]
    public void Renders_the_sort_slot_in_the_toolbar()
    {
        var cut = RenderComponent<FilterResults>(p => p
            .Add(c => c.FilteredCount, 1)
            .Add(c => c.TotalCount, 1)
            .Add(c => c.Noun, "articles")
            .Add(c => c.EmptyMessage, "none")
            .Add(c => c.Sort, b => b.AddMarkupContent(0, "<select class=\"my-sort\"></select>"))
            .Add(c => c.ChildContent, b => b.AddMarkupContent(0, "<div>card</div>")));

        Assert.NotNull(cut.Find("div.toolbar select.my-sort"));
    }
}
