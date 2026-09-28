using Microsoft.JSInterop;
using Respondeo.Services;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Services;

public class DiscoverBrowseStateTests
{
    private const string Root = "https://localhost/";

    [Fact]
    public void Query_round_trips_per_key()
    {
        var nav = new TestNavigationManager(Root + "discover/prayers");
        var js = new RecordingJs();
        using var state = new DiscoverBrowseState(nav, js);

        state.SetQuery("prayers", "mercy");

        Assert.Equal("mercy", state.GetQuery("prayers"));
        Assert.Equal(string.Empty, state.GetQuery("articles"));
    }

    [Fact]
    public void Filter_round_trips_per_key_and_is_independent()
    {
        var nav = new TestNavigationManager(Root + "discover/prayers");
        var js = new RecordingJs();
        using var state = new DiscoverBrowseState(nav, js);

        state.SetFilter("prayers.category", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "marian" });
        state.SetFilter("prayers.tags", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "rosary", "basic" });

        Assert.Contains("marian", state.GetFilter("prayers.category"));
        Assert.Equal(2, state.GetFilter("prayers.tags").Count);
        Assert.Empty(state.GetFilter("articles.topic"));
    }

    [Fact]
    public void GetFilter_returns_a_case_insensitive_set()
    {
        var nav = new TestNavigationManager(Root + "discover/articles");
        var js = new RecordingJs();
        using var state = new DiscoverBrowseState(nav, js);

        state.SetFilter("articles.topic", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Sacraments" });

        Assert.Contains("sacraments", state.GetFilter("articles.topic"));
    }

    [Fact]
    public void Stored_filter_is_isolated_from_the_caller_set()
    {
        var nav = new TestNavigationManager(Root + "discover/devotions");
        var js = new RecordingJs();
        using var state = new DiscoverBrowseState(nav, js);

        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "rosary" };
        state.SetFilter("devotions.kind", selected);

        // Mutating the caller's set afterwards must not change the remembered selection.
        selected.Add("chaplet");

        Assert.Single(state.GetFilter("devotions.kind"));
        Assert.Contains("rosary", state.GetFilter("devotions.kind"));
    }

    [Fact]
    public void Query_and_filters_are_preserved_when_drilling_into_an_item()
    {
        var nav = new TestNavigationManager(Root + "discover/prayers");
        var js = new RecordingJs();
        using var state = new DiscoverBrowseState(nav, js);
        state.SetQuery("prayers", "hail");
        state.SetFilter("prayers.category", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "marian" });

        // Individual prayer pages live under discover/ and still count as "in Discover".
        nav.NavigateTo("discover/prayers/hail-mary");

        Assert.Equal("hail", state.GetQuery("prayers"));
        Assert.Contains("marian", state.GetFilter("prayers.category"));
        Assert.DoesNotContain("respondeoDiscoverBrowse.clear", js.Calls);
    }

    [Fact]
    public void Leaving_the_discover_area_resets_query_and_filters_and_clears_the_js_store()
    {
        var nav = new TestNavigationManager(Root + "discover/prayers");
        var js = new RecordingJs();
        using var state = new DiscoverBrowseState(nav, js);
        state.SetQuery("prayers", "mercy");
        state.SetFilter("prayers.tags", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "rosary" });

        nav.NavigateTo("why-god");

        Assert.Equal(string.Empty, state.GetQuery("prayers"));
        Assert.Empty(state.GetFilter("prayers.tags"));
        Assert.Contains("respondeoDiscoverBrowse.clear", js.Calls);
    }

    [Fact]
    public void A_route_that_merely_starts_with_discover_is_not_treated_as_in_discover()
    {
        var nav = new TestNavigationManager(Root + "discover/prayers");
        var js = new RecordingJs();
        using var state = new DiscoverBrowseState(nav, js);
        state.SetQuery("prayers", "creed");

        // "discovers" (or similar) should not be mistaken for the Discover area.
        nav.NavigateTo("discovers");

        Assert.Equal(string.Empty, state.GetQuery("prayers"));
        Assert.Contains("respondeoDiscoverBrowse.clear", js.Calls);
    }

    [Fact]
    public void Dispose_unsubscribes_so_later_navigations_do_not_reset()
    {
        var nav = new TestNavigationManager(Root + "discover/prayers");
        var js = new RecordingJs();
        var state = new DiscoverBrowseState(nav, js);
        state.SetQuery("prayers", "glory");

        state.Dispose();
        nav.NavigateTo("why-god");

        Assert.Equal("glory", state.GetQuery("prayers"));
        Assert.DoesNotContain("respondeoDiscoverBrowse.clear", js.Calls);
    }

    /// <summary>Minimal IJSRuntime that records the identifiers it is asked to invoke.</summary>
    private sealed class RecordingJs : IJSRuntime
    {
        public List<string> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Calls.Add(identifier);
            return ValueTask.FromResult(default(TValue)!);
        }
    }
}
