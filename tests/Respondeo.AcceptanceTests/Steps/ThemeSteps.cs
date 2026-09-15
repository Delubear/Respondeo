using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

[Binding]
public sealed class ThemeSteps(PlaywrightContext context)
{
    private IPage Page => context.Page;

    [When("I toggle the theme")]
    public async Task WhenIToggleTheTheme()
    {
        await Page.Locator(".theme-toggle").ClickAsync();
    }

    [When("I reload the page")]
    public async Task WhenIReloadThePage()
    {
        await Page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync(".theme-toggle");
    }

    [Then("the page should use the light theme")]
    public async Task ThenThePageShouldUseTheLightTheme()
    {
        await AssertThemeAsync("light");
    }

    [Then("the page should use the dark theme")]
    public async Task ThenThePageShouldUseTheDarkTheme()
    {
        await AssertThemeAsync("dark");
    }

    // The theme is applied by setting data-theme on the document root; assert on that.
    private async Task AssertThemeAsync(string expected)
    {
        var theme = await Page.Locator("html").GetAttributeAsync("data-theme");
        Assert.Equal(expected, theme);
    }
}
