using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Components.Guides;
using Respondeo.LiturgyOfTheHours;
using Respondeo.Services;

namespace Respondeo.UnitTests.Components;

public class HourSettingsGuideTests : TestContext
{
    [Fact]
    public void Lauds_psalm_omission_is_a_distinct_persisted_preference_not_an_already_prayed_claim()
    {
        ComponentFactories.AddStub<FloatingGuide>(parameters => builder => builder.AddContent(0,
            parameters.Get(component => component.ChildContent)(new FloatingGuideContext(() => Task.CompletedTask))));
        var settings = Substitute.For<IHourPlayerSettingsService>();
        var current = new HourPlayerSettings { InvitatoryMode = InvitatoryMode.Specific, InvitatoryPsalm = "Psalm 67" };
        settings.Current.Returns(_ => current);
        settings.UpdateAsync(Arg.Any<Func<HourPlayerSettings, HourPlayerSettings>>()).Returns(call =>
        {
            current = call.Arg<Func<HourPlayerSettings, HourPlayerSettings>>()(current);
            settings.Changed += Raise.Event<Action>();
            return Task.CompletedTask;
        });
        Services.AddSingleton(settings);
        Services.AddSingleton(new FeatureFlags());
        var cut = RenderComponent<HourSettingsGuide>();
        var omitLabel = cut.FindAll("label").Single(label => label.TextContent.Contains("GILH 35", StringComparison.Ordinal));
        var ordinaryLabel = cut.FindAll("label").Single(label => label.TextContent.Contains("already prayed", StringComparison.Ordinal));

        omitLabel.QuerySelector("input")!.Change(true);

        Assert.Equal(InvitatoryMode.OmitPsalm, current.InvitatoryMode);
        Assert.Equal("Psalm 67", current.InvitatoryPsalm);
        Assert.Contains("keep \"Lord, open my lips\"", omitLabel.TextContent);
        cut.WaitForAssertion(() => Assert.True(cut.FindAll("label").Single(label => label.TextContent.Contains("GILH 35", StringComparison.Ordinal))
            .QuerySelector("input")!.HasAttribute("checked")));
        Assert.DoesNotContain("already prayed", omitLabel.TextContent);

        ordinaryLabel.QuerySelector("input")!.Change(true);

        Assert.Equal(InvitatoryMode.None, current.InvitatoryMode);
        Assert.Equal("Psalm 67", current.InvitatoryPsalm);
        Assert.Equal(current, HourPlayerSettingsService.Deserialize(System.Text.Json.JsonSerializer.Serialize(current)));
    }
}
