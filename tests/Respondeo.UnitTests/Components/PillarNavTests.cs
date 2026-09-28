using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Components;
using Respondeo.Services;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Components;

public class PillarNavTests : TestContext
{
    private const string Root = "https://localhost/";

    private void RegisterServices(FeatureFlags flags, string relativePath = "")
    {
        Services.AddSingleton<NavigationManager>(new TestNavigationManager(Root + relativePath));
        Services.AddSingleton(new NavigationIntent());
        Services.AddSingleton(Substitute.For<IBreadcrumbTrail>());
        Services.AddSingleton(flags);
    }

    [Fact]
    public void Shows_all_pillars_when_every_flag_is_on()
    {
        RegisterServices(new FeatureFlags());

        var cut = RenderComponent<PillarNav>();

        var labels = cut.FindAll(".pillarnav__link").Select(l => l.TextContent.Trim()).ToList();
        Assert.Equal(3, labels.Count);
        Assert.Contains("Summa Theologiae", labels);
        Assert.Contains("Discover Catholicism", labels);
    }

    [Fact]
    public void Hides_summa_pillar_when_summa_flag_is_off()
    {
        RegisterServices(new FeatureFlags { SummaPillar = false });

        var cut = RenderComponent<PillarNav>();

        var labels = cut.FindAll(".pillarnav__link").Select(l => l.TextContent.Trim()).ToList();
        Assert.DoesNotContain("Summa Theologiae", labels);
        Assert.Contains("Discover Catholicism", labels);
    }

    [Fact]
    public void Hides_discover_pillar_when_master_switch_is_off_even_if_subfeatures_on()
    {
        RegisterServices(new FeatureFlags { DiscoverPillar = false });

        var cut = RenderComponent<PillarNav>();

        var labels = cut.FindAll(".pillarnav__link").Select(l => l.TextContent.Trim()).ToList();
        Assert.DoesNotContain("Discover Catholicism", labels);
    }

    [Fact]
    public void Hides_discover_pillar_when_master_switch_on_but_all_subfeatures_off()
    {
        RegisterServices(new FeatureFlags
        {
            DiscoverPillar = true,
            MiraclesFeature = false,
            PrayerFeature = false,
            DevotionsFeature = false,
            ArticlesFeature = false,
        });

        var cut = RenderComponent<PillarNav>();

        var labels = cut.FindAll(".pillarnav__link").Select(l => l.TextContent.Trim()).ToList();
        Assert.DoesNotContain("Discover Catholicism", labels);
    }

    [Fact]
    public void Shows_discover_pillar_when_master_switch_on_and_one_subfeature_on()
    {
        RegisterServices(new FeatureFlags
        {
            DiscoverPillar = true,
            MiraclesFeature = false,
            PrayerFeature = true,
            DevotionsFeature = false,
            ArticlesFeature = false,
        });

        var cut = RenderComponent<PillarNav>();

        var labels = cut.FindAll(".pillarnav__link").Select(l => l.TextContent.Trim()).ToList();
        Assert.Contains("Discover Catholicism", labels);
    }

    [Fact]
    public void Journey_pillar_is_always_shown()
    {
        RegisterServices(new FeatureFlags
        {
            SummaPillar = false,
            DiscoverPillar = false,
        });

        var cut = RenderComponent<PillarNav>();

        var labels = cut.FindAll(".pillarnav__link").Select(l => l.TextContent.Trim()).ToList();
        Assert.Single(labels);
        Assert.Contains("Inquiry", labels);
    }
}
