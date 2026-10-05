using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

[Binding]
public sealed class SaintsSteps(PlaywrightContext context)
{
    private IPage Page => context.Page;

    [Given("I open the saints browser")]
    public async Task GivenIOpenTheSaintsBrowser()
    {
        await Page.GotoAsync($"{context.BaseUrl}/discover/saints", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync("a.card");
    }

    [When("I choose the first saint")]
    public async Task WhenIChooseTheFirstSaint()
    {
        await Page.Locator("a.card").First.ClickAsync();
        await Page.WaitForSelectorAsync("article.content-article");
    }

    [Then("I should see at least one saint card")]
    public async Task ThenIShouldSeeAtLeastOneSaintCard()
    {
        var count = await Page.Locator("a.card").CountAsync();
        Assert.True(count >= 1, $"Expected at least one saint card, found {count}.");
    }

    [Then("the saint profile should be visible")]
    public async Task ThenTheSaintProfileShouldBeVisible()
    {
        Assert.True(await Page.Locator("article.content-article").First.IsVisibleAsync());
    }

    [Then("the saint facts should be visible")]
    public async Task ThenTheSaintFactsShouldBeVisible()
    {
        Assert.True(await Page.Locator(".saint__facts").First.IsVisibleAsync());
    }
}
