using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class SummaBrowseStateTests
{
    private const string Root = "https://localhost/";

    [Fact]
    public void Query_is_preserved_when_navigating_within_the_summa_list_and_search()
    {
        var nav = new TestNavigationManager(Root + "summa");
        var js = new RecordingJs();
        using var state = new SummaBrowseState(nav, js) { Query = "grace" };

        // Moving between the browse list and a search stays within /summa.
        nav.NavigateTo("summa");

        Assert.Equal("grace", state.Query);
        Assert.DoesNotContain("respondeoSummaBrowse.clear", js.Calls);
    }

    [Fact]
    public void Query_is_preserved_when_drilling_into_a_question()
    {
        var nav = new TestNavigationManager(Root + "summa");
        var js = new RecordingJs();
        using var state = new SummaBrowseState(nav, js) { Query = "virtue" };

        // Individual question pages live at summa/{id} and still count as "in Summa".
        nav.NavigateTo("summa/prima-q002");

        Assert.Equal("virtue", state.Query);
        Assert.DoesNotContain("respondeoSummaBrowse.clear", js.Calls);
    }

    [Fact]
    public void Query_is_preserved_on_a_part_scoped_route()
    {
        var nav = new TestNavigationManager(Root + "summa");
        var js = new RecordingJs();
        using var state = new SummaBrowseState(nav, js) { Query = "law" };

        nav.NavigateTo("summa/part/prima");

        Assert.Equal("law", state.Query);
        Assert.DoesNotContain("respondeoSummaBrowse.clear", js.Calls);
    }

    [Fact]
    public void Leaving_the_summa_area_resets_query_and_clears_the_js_store()
    {
        var nav = new TestNavigationManager(Root + "summa");
        var js = new RecordingJs();
        using var state = new SummaBrowseState(nav, js) { Query = "existence of God" };

        nav.NavigateTo("why-god/node/root");

        Assert.Equal(string.Empty, state.Query);
        Assert.Contains("respondeoSummaBrowse.clear", js.Calls);
    }

    [Fact]
    public void A_route_that_merely_starts_with_summa_is_not_treated_as_in_summa()
    {
        var nav = new TestNavigationManager(Root + "summa");
        var js = new RecordingJs();
        using var state = new SummaBrowseState(nav, js) { Query = "angels" };

        // "summaries" should not be mistaken for the Summa area.
        nav.NavigateTo("summaries");

        Assert.Equal(string.Empty, state.Query);
        Assert.Contains("respondeoSummaBrowse.clear", js.Calls);
    }

    [Fact]
    public void Navigating_to_the_home_route_resets_state()
    {
        var nav = new TestNavigationManager(Root + "summa");
        var js = new RecordingJs();
        using var state = new SummaBrowseState(nav, js) { Query = "sacraments" };

        nav.NavigateTo("");

        Assert.Equal(string.Empty, state.Query);
        Assert.Contains("respondeoSummaBrowse.clear", js.Calls);
    }

    [Fact]
    public void Dispose_unsubscribes_so_later_navigations_do_not_reset()
    {
        var nav = new TestNavigationManager(Root + "summa");
        var js = new RecordingJs();
        var state = new SummaBrowseState(nav, js) { Query = "prudence" };

        state.Dispose();
        nav.NavigateTo("why-god/node/root");

        Assert.Equal("prudence", state.Query);
        Assert.DoesNotContain("respondeoSummaBrowse.clear", js.Calls);
    }

    /// <summary>Minimal NavigationManager that lets tests drive LocationChanged via NavigateTo.</summary>
    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string uri) => Initialize(Root, uri);

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            var absolute = ToAbsoluteUri(uri).ToString();
            Uri = absolute;
            NotifyLocationChanged(isInterceptedLink: false);
        }
    }

    /// <summary>Minimal IJSRuntime that records the identifiers it is asked to invoke.</summary>
    private sealed class RecordingJs : IJSRuntime
    {
        public List<string> Calls { get; } = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Calls.Add(identifier);
            return ValueTask.FromResult(default(TValue)!);
        }
    }
}
