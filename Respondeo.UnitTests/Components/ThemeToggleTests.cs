using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Components;
using Respondeo.Services;
using Xunit;

namespace Respondeo.UnitTests.Components;

public class ThemeToggleTests : TestContext
{
    [Fact]
    public void Initializes_the_theme_on_first_render()
    {
        var theme = Substitute.For<IThemeService>();
        theme.Current.Returns(Theme.Light);
        Services.AddSingleton(theme);

        RenderComponent<ThemeToggle>();

        theme.Received(1).InitializeAsync();
    }

    [Fact]
    public void Shows_moon_and_dark_label_when_current_is_light()
    {
        var theme = Substitute.For<IThemeService>();
        theme.Current.Returns(Theme.Light);
        Services.AddSingleton(theme);

        var cut = RenderComponent<ThemeToggle>();

        var button = cut.Find("button.theme-toggle");
        Assert.Equal("Switch to dark theme", button.GetAttribute("aria-label"));
        Assert.Contains("\u263E", cut.Find(".theme-toggle__glyph").TextContent);
    }

    [Fact]
    public void Shows_sun_and_light_label_when_current_is_dark()
    {
        var theme = Substitute.For<IThemeService>();
        theme.Current.Returns(Theme.Dark);
        Services.AddSingleton(theme);

        var cut = RenderComponent<ThemeToggle>();

        var button = cut.Find("button.theme-toggle");
        Assert.Equal("Switch to light theme", button.GetAttribute("aria-label"));
        Assert.Contains("\u2600", cut.Find(".theme-toggle__glyph").TextContent);
    }

    [Fact]
    public void Clicking_the_button_toggles_the_theme()
    {
        var theme = Substitute.For<IThemeService>();
        theme.Current.Returns(Theme.Light);
        Services.AddSingleton(theme);
        var cut = RenderComponent<ThemeToggle>();

        cut.Find("button.theme-toggle").Click();

        theme.Received(1).ToggleAsync();
    }
}
