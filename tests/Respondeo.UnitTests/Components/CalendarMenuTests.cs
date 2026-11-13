using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Components.Guides;
using Respondeo.LiturgicalCalendar;
using Respondeo.Services;
using Respondeo.Services.Calendar;

namespace Respondeo.UnitTests.Components;

public class CalendarMenuTests : TestContext
{
    [Fact]
    public void Disabled_calendar_feature_hides_the_selector_without_loading_the_preference()
    {
        var preference = Substitute.For<ICalendarPreference>();
        Services.AddSingleton(preference);
        Services.AddSingleton(new FeatureFlags { LiturgicalCalendarFeature = false });

        var cut = RenderComponent<CalendarMenu>();

        Assert.Empty(cut.Markup);
        _ = preference.DidNotReceive().InitializeAsync();
    }

    [Fact]
    public void Enabled_calendar_feature_shows_the_selector_and_loads_the_preference()
    {
        var preference = Substitute.For<ICalendarPreference>();
        preference.Options.Returns(LiturgicalCalendarOptions.Default);
        preference.InitializeAsync().Returns(Task.CompletedTask);
        Services.AddSingleton(preference);
        Services.AddSingleton(new FeatureFlags { LiturgicalCalendarFeature = true });

        var cut = RenderComponent<CalendarMenu>();

        Assert.Contains("General Roman Calendar", cut.Find("summary").TextContent);
        Assert.Equal(2, cut.FindAll("input[type=radio]").Count);
        _ = preference.Received(1).InitializeAsync();
    }
}
