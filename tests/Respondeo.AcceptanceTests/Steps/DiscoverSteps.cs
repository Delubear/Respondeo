using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

[Binding]
public sealed class DiscoverSteps(PlaywrightContext context)
{
    private IPage Page => context.Page;

    [Given("I open the Discover landing page")]
    public async Task GivenIOpenTheDiscoverLandingPage()
    {
        await Page.GotoAsync($"{context.BaseUrl}/discover", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync(".pillar__title");
    }

    [Given("I open the prayers browser")]
    public async Task GivenIOpenThePrayersBrowser()
    {
        await Page.GotoAsync($"{context.BaseUrl}/discover/prayers", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync("a.card");
    }

    [Given("I open the articles browser")]
    public async Task GivenIOpenTheArticlesBrowser()
    {
        await Page.GotoAsync($"{context.BaseUrl}/discover/articles", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync("a.card");
    }

    [When("I choose the first prayer")]
    public async Task WhenIChooseTheFirstPrayer()
    {
        await Page.Locator("a.card").First.ClickAsync();
        await Page.WaitForSelectorAsync(".prayer__text");
    }

    [When("I choose the first article")]
    public async Task WhenIChooseTheFirstArticle()
    {
        await Page.Locator("a.card").First.ClickAsync();
        await Page.WaitForSelectorAsync(".content-article .content-body");
    }

    [Then("I should see a link to \"(.*)\"")]
    public async Task ThenIShouldSeeALinkTo(string href)
    {
        var count = await Page.Locator($"a[href='{href}']").CountAsync();
        Assert.True(count >= 1, $"Expected at least one link to '{href}', found {count}.");
    }

    [Then("the Discover sub-navigation should be visible")]
    public async Task ThenTheDiscoverSubNavigationShouldBeVisible()
    {
        var nav = Page.Locator("nav[aria-label='Discover Catholicism']");
        Assert.True(await nav.IsVisibleAsync(), "Expected the Discover sub-navigation to be visible.");
    }

    [Then("the prayer text should be visible")]
    public async Task ThenThePrayerTextShouldBeVisible()
    {
        Assert.True(await Page.Locator(".prayer__text").First.IsVisibleAsync());
    }

    [Then("the article body should be visible")]
    public async Task ThenTheArticleBodyShouldBeVisible()
    {
        Assert.True(await Page.Locator(".content-article .content-body").First.IsVisibleAsync());
    }
}
