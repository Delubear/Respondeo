using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

[Binding]
public sealed class PrayerGuideSteps(PlaywrightContext context)
{
    private IPage Page => context.Page;

    [When("I open the prayer guide")]
    public async Task WhenIOpenThePrayerGuide()
    {
        await Page.Locator(".prayer-guide__trigger").First.ClickAsync();
        await Page.WaitForSelectorAsync(".dialog[open]");
    }

    [When("I close the prayer guide")]
    public async Task WhenICloseThePrayerGuide()
    {
        await Page.Locator(".dialog[open] .dialog__close").ClickAsync();
        await Page.WaitForSelectorAsync(".dialog[open]", new PageWaitForSelectorOptions { State = WaitForSelectorState.Detached });
    }

    [Given("I open the \"(.*)\" prayer directly")]
    public async Task GivenIOpenThePrayerDirectly(string slug)
    {
        await Page.GotoAsync($"{context.BaseUrl}/{slug}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync(".prayer__text");
    }

    [Then("the prayer guide dialog should be visible")]
    public async Task ThenThePrayerGuideDialogShouldBeVisible()
    {
        Assert.True(await Page.Locator(".dialog[open]").IsVisibleAsync());
    }

    [Then("the prayer guide dialog should explain the versicle and response")]
    public async Task ThenThePrayerGuideDialogShouldExplainTheVersicleAndResponse()
    {
        var body = await Page.Locator(".dialog[open] .dialog__body").InnerTextAsync();
        Assert.Contains("versicle", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("response", body, StringComparison.OrdinalIgnoreCase);
    }

    [Then("the prayer guide dialog should not be visible")]
    public async Task ThenThePrayerGuideDialogShouldNotBeVisible()
    {
        Assert.Equal(0, await Page.Locator(".dialog[open]").CountAsync());
    }

    [Then("the prayer should show at least one embedded prayer")]
    public async Task ThenThePrayerShouldShowAtLeastOneEmbeddedPrayer()
    {
        await Page.WaitForSelectorAsync(".prayer-embed");
        var count = await Page.Locator(".prayer-embed").CountAsync();
        Assert.True(count >= 1, $"Expected at least one embedded prayer, found {count}.");
    }
}
