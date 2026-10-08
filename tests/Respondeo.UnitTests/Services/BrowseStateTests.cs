using Respondeo.Services;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Services;

public class BrowseStateTests
{
    private const string Root = "https://localhost/";

    [Fact]
    public void Query_round_trips_per_section()
    {
        var nav = new TestNavigationManager(Root + "discover/prayers");
        using var state = new BrowseState(nav);
        var prayers = state.Section("discover/prayers");
        var articles = state.Section("discover/articles");

        prayers.Query = "mercy";

        Assert.Equal("mercy", prayers.Query);
        Assert.Equal(string.Empty, articles.Query);
    }

    [Fact]
    public void Section_returns_the_same_instance_for_the_same_area()
    {
        var nav = new TestNavigationManager(Root + "discover/prayers");
        using var state = new BrowseState(nav);

        var first = state.Section("discover/prayers");
        var second = state.Section("discover/prayers");

        Assert.Same(first, second);
    }

    [Fact]
    public void Filter_round_trips_per_facet_and_is_independent()
    {
        var nav = new TestNavigationManager(Root + "discover/prayers");
        using var state = new BrowseState(nav);
        var prayers = state.Section("discover/prayers");

        prayers.SetFilter("category", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "marian" });
        prayers.SetFilter("tags", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "rosary", "basic" });

        Assert.Contains("marian", prayers.GetFilter("category"));
        Assert.Equal(2, prayers.GetFilter("tags").Count);
        Assert.Empty(prayers.GetFilter("missing"));
    }

    [Fact]
    public void GetFilter_returns_a_case_insensitive_set()
    {
        var nav = new TestNavigationManager(Root + "discover/articles");
        using var state = new BrowseState(nav);
        var articles = state.Section("discover/articles");

        articles.SetFilter("tag", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Sacraments" });

        Assert.Contains("sacraments", articles.GetFilter("tag"));
    }

    [Fact]
    public void Stored_filter_is_isolated_from_the_caller_set()
    {
        var nav = new TestNavigationManager(Root + "discover/devotions");
        using var state = new BrowseState(nav);
        var devotions = state.Section("discover/devotions");

        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "rosary" };
        devotions.SetFilter("kind", selected);

        // Mutating the caller's set afterwards must not change the remembered selection.
        selected.Add("chaplet");

        Assert.Single(devotions.GetFilter("kind"));
        Assert.Contains("rosary", devotions.GetFilter("kind"));
    }

    [Fact]
    public void Query_and_filters_are_preserved_when_drilling_into_an_item()
    {
        var nav = new TestNavigationManager(Root + "discover/prayers");
        var calls = new List<string>();
        using var state = new BrowseState(nav);
        var prayers = state.Section("discover/prayers", () => { calls.Add("exit"); return ValueTask.CompletedTask; });
        prayers.Query = "hail";
        prayers.SetFilter("category", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "marian" });

        // Individual prayer pages live under discover/prayers and still count as "in the section".
        nav.NavigateTo("discover/prayers/hail-mary");

        Assert.Equal("hail", prayers.Query);
        Assert.Contains("marian", prayers.GetFilter("category"));
        Assert.Empty(calls);
    }

    [Fact]
    public void Leaving_a_sub_area_resets_only_that_section_and_fires_its_exit_callback()
    {
        var nav = new TestNavigationManager(Root + "discover/prayers");
        var prayerExits = new List<string>();
        var articleExits = new List<string>();
        using var state = new BrowseState(nav);
        var prayers = state.Section("discover/prayers", () => { prayerExits.Add("exit"); return ValueTask.CompletedTask; });
        var articles = state.Section("discover/articles", () => { articleExits.Add("exit"); return ValueTask.CompletedTask; });
        prayers.Query = "mercy";
        prayers.SetFilter("tags", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "rosary" });
        articles.Query = "grace";

        // Navigating from prayers to articles leaves the prayers sub-area but not articles.
        nav.NavigateTo("discover/articles");

        Assert.Equal(string.Empty, prayers.Query);
        Assert.Empty(prayers.GetFilter("tags"));
        Assert.Single(prayerExits);

        Assert.Equal("grace", articles.Query);
        Assert.Empty(articleExits);
    }

    [Fact]
    public void Leaving_the_summa_area_resets_query_and_fires_the_exit_callback()
    {
        var nav = new TestNavigationManager(Root + "summa");
        var exits = new List<string>();
        using var state = new BrowseState(nav);
        var summa = state.Section("summa", () => { exits.Add("exit"); return ValueTask.CompletedTask; });
        summa.Query = "existence of God";

        nav.NavigateTo("why-god/root");

        Assert.Equal(string.Empty, summa.Query);
        Assert.Single(exits);
    }

    [Fact]
    public void Query_is_preserved_when_drilling_into_a_summa_question()
    {
        var nav = new TestNavigationManager(Root + "summa");
        var exits = new List<string>();
        using var state = new BrowseState(nav);
        var summa = state.Section("summa", () => { exits.Add("exit"); return ValueTask.CompletedTask; });
        summa.Query = "virtue";

        nav.NavigateTo("summa/prima-q002");

        Assert.Equal("virtue", summa.Query);
        Assert.Empty(exits);
    }

    [Fact]
    public void A_route_that_merely_starts_with_the_area_is_not_treated_as_in_the_area()
    {
        var nav = new TestNavigationManager(Root + "summa");
        var exits = new List<string>();
        using var state = new BrowseState(nav);
        var summa = state.Section("summa", () => { exits.Add("exit"); return ValueTask.CompletedTask; });
        summa.Query = "angels";

        // "summaries" should not be mistaken for the Summa area.
        nav.NavigateTo("summaries");

        Assert.Equal(string.Empty, summa.Query);
        Assert.Single(exits);
    }

    [Fact]
    public void HasActiveFilters_reflects_query_and_facets()
    {
        var nav = new TestNavigationManager(Root + "discover/miracles");
        using var state = new BrowseState(nav);
        var miracles = state.Section("discover/miracles");

        Assert.False(miracles.HasActiveFilters);

        miracles.SetFilter("type", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "eucharistic" });
        Assert.True(miracles.HasActiveFilters);

        miracles.Clear();
        Assert.False(miracles.HasActiveFilters);
    }

    [Fact]
    public void Dispose_unsubscribes_so_later_navigations_do_not_reset()
    {
        var nav = new TestNavigationManager(Root + "discover/prayers");
        var exits = new List<string>();
        var state = new BrowseState(nav);
        var prayers = state.Section("discover/prayers", () => { exits.Add("exit"); return ValueTask.CompletedTask; });
        prayers.Query = "glory";

        state.Dispose();
        nav.NavigateTo("why-god");

        Assert.Equal("glory", prayers.Query);
        Assert.Empty(exits);
    }
}
