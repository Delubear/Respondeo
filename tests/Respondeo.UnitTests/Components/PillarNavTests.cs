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
        Assert.Equal(4, labels.Count);
        Assert.Contains("Summa Theologiae", labels);
        Assert.Contains("Holy Bible", labels);
        Assert.Contains("Discover Catholicism", labels);
    }
}
