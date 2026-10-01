using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

[Binding]
public sealed class DevotionSteps(PlaywrightContext context)
{
    private IPage Page => context.Page;

    [Given("I open the devotions browser")]
    public async Task GivenIOpenTheDevotionsBrowser()
    {
        await Page.GotoAsync($"{context.BaseUrl}/discover/devotions", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync("a.card");
    }

    [When("I choose the first devotion")]
    public async Task WhenIChooseTheFirstDevotion()
    {
        await Page.Locator("a.card").First.ClickAsync();
        await Page.WaitForSelectorAsync(".devotion__set-options");
    }

    [When("I begin praying")]
    public async Task WhenIBeginPraying()
    {
        await Page.Locator(".devotion__set-options .card").First.ClickAsync();
        await Page.WaitForSelectorAsync(".devotion--praying");
    }

    [Then("I should see at least one devotion card")]
    public async Task ThenIShouldSeeAtLeastOneDevotionCard()
    {
        var count = await Page.Locator("a.card").CountAsync();
        Assert.True(count >= 1, $"Expected at least one devotion card, found {count}.");
    }

    [Then("the devotion intro should be visible")]
    public async Task ThenTheDevotionIntroShouldBeVisible()
    {
        Assert.True(await Page.Locator(".devotion__set-options").First.IsVisibleAsync());
    }

    [Then("I should see a way to begin praying")]
    public async Task ThenIShouldSeeAWayToBeginPraying()
    {
        Assert.True(await Page.Locator(".devotion__set-options .card").First.IsVisibleAsync());
    }

    [Then("the praying guide should be visible")]
    public async Task ThenThePrayingGuideShouldBeVisible()
    {
        Assert.True(await Page.Locator(".devotion__list").First.IsVisibleAsync());
    }

    [Then("I should see a way to leave the devotion")]
    public async Task ThenIShouldSeeAWayToLeaveTheDevotion()
    {
        var leave = Page.Locator(".devotion__exit a[href='discover/devotions']");
        Assert.True(await leave.First.IsVisibleAsync(), "Expected a link to leave the devotion.");
    }
}
