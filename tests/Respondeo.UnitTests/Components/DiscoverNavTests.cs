using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Components;
using Respondeo.Services;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Components;

public class DiscoverNavTests : TestContext
{
    private const string Root = "https://localhost/";

    private void RegisterServices(FeatureFlags flags, string relativePath)
    {
        Services.AddSingleton<NavigationManager>(new TestNavigationManager(Root + relativePath));
        Services.AddSingleton(new NavigationIntent());
        Services.AddSingleton(Substitute.For<IBreadcrumbTrail>());
        Services.AddSingleton(flags);
    }

    [Fact]
    public void Does_not_render_outside_the_discover_pillar()
    {
        RegisterServices(new FeatureFlags(), "summa");

        var cut = RenderComponent<DiscoverNav>();

        Assert.Empty(cut.FindAll(".mastnav"));
    }

    [Fact]
    public void Shows_all_tabs_when_every_flag_is_on()
    {
        RegisterServices(new FeatureFlags(), "discover");

        var cut = RenderComponent<DiscoverNav>();

        var labels = cut.FindAll(".mastnav__link").Select(l => l.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Overview", "Miracles", "Prayers", "Devotions", "Articles" }, labels);
    }

    [Fact]
    public void Renders_no_tabs_when_master_switch_is_off()
    {
        RegisterServices(new FeatureFlags { DiscoverPillar = false }, "discover");

        var cut = RenderComponent<DiscoverNav>();

        Assert.Empty(cut.FindAll(".mastnav__link"));
    }

    [Fact]
    public void Hides_a_subarea_tab_when_its_flag_is_off()
    {
        RegisterServices(new FeatureFlags { MiraclesFeature = false }, "discover");

        var cut = RenderComponent<DiscoverNav>();

        var labels = cut.FindAll(".mastnav__link").Select(l => l.TextContent.Trim()).ToList();
        Assert.DoesNotContain("Miracles", labels);
        Assert.Contains("Overview", labels);
        Assert.Contains("Prayers", labels);
    }

    [Fact]
    public void Overview_tab_shows_whenever_the_pillar_is_active_and_master_switch_on()
    {
        RegisterServices(new FeatureFlags
        {
            MiraclesFeature = false,
            PrayerFeature = false,
            DevotionsFeature = false,
            ArticlesFeature = false,
        }, "discover");

        var cut = RenderComponent<DiscoverNav>();

        var labels = cut.FindAll(".mastnav__link").Select(l => l.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Overview" }, labels);
    }
}
