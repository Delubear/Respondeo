using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Services.Navigation;

/// <summary>Characterizes where <see cref="ScrollCoordinator"/> lands the window after each kind of navigation.</summary>
public class ScrollCoordinatorTests : TestContext
{
    private readonly FakeNavigationManager _nav;
    private readonly NavigationIntent _intent;
    private readonly ScrollCoordinator _sut;

    public ScrollCoordinatorTests()
    {
        this.AddScrollCoordinator();
        _nav = Services.GetRequiredService<FakeNavigationManager>();
        _intent = Services.GetRequiredService<NavigationIntent>();
        _sut = Services.GetRequiredService<ScrollCoordinator>();
    }

    private IReadOnlyList<object?[]> Landings() =>
        JSInterop.Invocations
            .Where(i => i.Identifier == "respondeoScroll.land")
            .Select(i => i.Arguments.ToArray())
            .ToList();

    [Fact]
    public async Task NewPath_LandsOnContent()
    {
        _nav.NavigateTo("why-god");
        await _sut.ApplyDefaultAsync();

        var landing = Assert.Single(Landings());
        Assert.Equal(new object?[] { "content", false, false }, landing);
    }

    [Fact]
    public async Task ResetToTop_LandsOnTop()
    {
        _intent.ResetToTop = true;
        _nav.NavigateTo("why-god");
        await _sut.ApplyDefaultAsync();

        Assert.Equal("top", Assert.Single(Landings())[0]);
        Assert.False(_intent.ResetToTop);
    }

    [Fact]
    public async Task QueryOnlyChange_GlidesToContentImmediately_AndSkipsDefault()
    {
        _nav.NavigateTo("why-god");
        await _sut.ApplyDefaultAsync();
        _nav.NavigateTo("why-god?x=1");
        await _sut.ApplyDefaultAsync();

        var landings = Landings();
        Assert.Equal(2, landings.Count);
        Assert.Equal(new object?[] { "content", true }, landings[1]);
    }

    [Fact]
    public async Task Claim_SuppressesDefault()
    {
        _nav.NavigateTo("why-god");
        _sut.Claim();
        await _sut.ApplyDefaultAsync();

        Assert.Empty(Landings());
    }

    [Fact]
    public async Task Defer_HoldsDefaultUntilApplied_ThenLandsOnce()
    {
        _nav.NavigateTo("why-god");
        _sut.Defer();
        await _sut.ApplyDefaultAsync();
        Assert.Empty(Landings());

        await _sut.ApplyDeferredAsync();
        await _sut.ApplyDeferredAsync();
        await _sut.ApplyDefaultAsync();

        Assert.Equal(new object?[] { "content", false, false }, Assert.Single(Landings()));
    }

    [Fact]
    public async Task Defer_CarriesResetToTop()
    {
        _intent.ResetToTop = true;
        _nav.NavigateTo("why-god");
        _sut.Defer();
        await _sut.ApplyDeferredAsync();

        Assert.Equal("top", Assert.Single(Landings())[0]);
    }

    [Fact]
    public async Task Defer_IsDiscardedByNextNavigation()
    {
        _nav.NavigateTo("why-god");
        _sut.Defer();
        _nav.NavigateTo("summa");
        await _sut.ApplyDeferredAsync();

        Assert.Empty(Landings());
    }

    [Fact]
    public async Task LandAt_LandsOnElement_AndSuppressesDefault()
    {
        _nav.NavigateTo("bible/john/3");
        _sut.Claim();
        await _sut.LandAtAsync("v16", center: true);
        await _sut.ApplyDefaultAsync();

        Assert.Equal(new object?[] { "v16", false, true }, Assert.Single(Landings()));
    }

    [Fact]
    public async Task NoNavigation_DoesNothing()
    {
        await _sut.ApplyDefaultAsync();

        Assert.Empty(Landings());
    }

    [Fact]
    public async Task ApplyDefault_LandsOnlyOnce()
    {
        _nav.NavigateTo("why-god");
        await _sut.ApplyDefaultAsync();
        await _sut.ApplyDefaultAsync();

        Assert.Single(Landings());
    }
}
