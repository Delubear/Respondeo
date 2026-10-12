using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

[Binding]
public sealed class MiracleSteps(PlaywrightContext context)
{
    private IPage Page => context.Page;

    [Given("I open the miracles browser")]
    public async Task GivenIOpenTheMiraclesBrowser()
    {
        await Page.GotoAsync($"{context.BaseUrl}/discover/miracles", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync("a.card");
    }

    [When("I choose the first miracle")]
    public async Task WhenIChooseTheFirstMiracle()
    {
        await Page.Locator("a.card").First.ClickAsync();
        await Page.WaitForSelectorAsync("article.content-article");
    }

    [Then("I should see at least one miracle card")]
    public async Task ThenIShouldSeeAtLeastOneMiracleCard()
    {
        var count = await Page.Locator("a.card").CountAsync();
        Assert.True(count >= 1, $"Expected at least one miracle card, found {count}.");
    }

    [Then("the miracle account should be visible")]
    public async Task ThenTheMiracleAccountShouldBeVisible()
    {
        Assert.True(await Page.Locator("article.content-article").First.IsVisibleAsync());
    }

    [Then("the miracle facts should be visible")]
    public async Task ThenTheMiracleFactsShouldBeVisible()
    {
        Assert.True(await Page.Locator(".miracle__facts").First.IsVisibleAsync());
    }
}
