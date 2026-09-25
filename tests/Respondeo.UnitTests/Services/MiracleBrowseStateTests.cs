using Microsoft.AspNetCore.Components;
using Respondeo.Content.Miracles;
using Respondeo.Services;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Services;

public class MiracleBrowseStateTests
{
    private const string Root = "https://localhost/";

    [Fact]
    public void Filters_are_preserved_when_navigating_within_the_miracles_area()
    {
        var nav = new TestNavigationManager(Root + "miracles");
        using var state = new MiracleBrowseState(nav) { Query = "lanciano" };
        state.ToggleType("eucharistic");

        // Drilling into a miracle detail page stays within /miracles.
        nav.NavigateTo("miracles/lanciano-eucharistic-miracle");

        Assert.Equal("lanciano", state.Query);
        Assert.Contains("eucharistic", state.Types);
    }

    [Fact]
    public void Filters_are_cleared_when_leaving_the_miracles_area()
    {
        var nav = new TestNavigationManager(Root + "miracles");
        using var state = new MiracleBrowseState(nav) { Query = "healing" };
        state.ToggleApproval("approved");

        nav.NavigateTo("why-god");

        Assert.Equal(string.Empty, state.Query);
        Assert.Empty(state.Approvals);
        Assert.False(state.HasActiveFilters);
    }

    [Fact]
    public void Toggling_a_facet_twice_removes_it()
    {
        var nav = new TestNavigationManager(Root + "miracles");
        using var state = new MiracleBrowseState(nav);

        state.ToggleRegion("europe");
        Assert.Contains("europe", state.Regions);

        state.ToggleRegion("europe");
        Assert.DoesNotContain("europe", state.Regions);
    }
}
