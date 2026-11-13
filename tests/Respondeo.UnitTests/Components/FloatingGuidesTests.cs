using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Respondeo.Components.Guides;
using Respondeo.Layout;
using Respondeo.Navigation;
using Respondeo.Services;

namespace Respondeo.UnitTests.Components;

public class FloatingGuidesTests : TestContext
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public void Liturgical_calendar_flag_controls_route_and_forced_orb(bool enabled, int expectedCount)
    {
        ComponentFactories.AddStub<LiturgicalOrb>();
        Services.AddSingleton(new FeatureFlags { LiturgicalCalendarFeature = enabled });

        var cut = RenderComponent<FloatingGuides>(parameters => parameters
            .Add(component => component.Guides, FloatingGuideRoutes.Resolve("discover", enabled))
            .Add(component => component.ForcedOrb, OrbLauncher.Liturgical));

        Assert.Equal(expectedCount, cut.FindComponents<Stub<LiturgicalOrb>>().Count);
    }
}
