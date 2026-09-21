using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

[Binding]
public sealed class NavigationSteps(PlaywrightContext context)
{
    private IPage Page => context.Page;

    [Given("I open the start page")]
    public async Task GivenIOpenTheStartPage()
    {
        // Blazor WebAssembly downloads its runtime after the load event, so wait for the network to settle before asserting the app has rendered its entry-point cards.
        await Page.GotoAsync(context.BaseUrl + "/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync(".card");
    }

    [When("I choose the first stage card")]
    public async Task WhenIChooseTheFirstStageCard()
    {
        // Landing on a stage page, which renders its branch cards (but no breadcrumb yet).
        await Page.Locator(".card").First.ClickAsync();
        await Page.WaitForSelectorAsync(".card");
    }

    [When("I choose the first branch card")]
    public async Task WhenIChooseTheFirstBranchCard()
    {
        await Page.Locator(".card").First.ClickAsync();
        await Page.WaitForSelectorAsync(".breadcrumb");
    }

    [When("I navigate back to the start page")]
    public async Task WhenINavigateBackToTheStartPage()
    {
        await Page.GotoAsync(context.BaseUrl + "/");
        await Page.WaitForSelectorAsync(".card");
    }

    [Given("I open the \"(.*)\" stage directly")]
    public async Task GivenIOpenTheStageDirectly(string slug)
    {
        await Page.GotoAsync($"{context.BaseUrl}/{slug}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync(".card");
    }

    [Given("I open the \"(.*)\" path directly")]
    public async Task GivenIOpenThePathDirectly(string slug)
    {
        await Page.GotoAsync($"{context.BaseUrl}/{slug}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    }

    [When("I choose the first masthead stage")]
    public async Task WhenIChooseTheFirstMastheadStage()
    {
        // The first masthead link is Home; the first stage link is the next one.
        // We may be on a node page whose branch cards are still in the DOM, so wait for the
        // client-side navigation to actually land on the stage page (which has no breadcrumb)
        // before proceeding, otherwise a later step could click a stale card mid-transition.
        await Page.Locator(".mastnav__link").Nth(1).ClickAsync();
        await Page.WaitForSelectorAsync(".breadcrumb", new PageWaitForSelectorOptions { State = WaitForSelectorState.Detached });
        await Page.WaitForSelectorAsync(".card");
    }

    [Then("I should see at least one stage card")]
    public async Task ThenIShouldSeeAtLeastOneStageCard()
    {
        var count = await Page.Locator(".card").CountAsync();
        Assert.True(count >= 1, $"Expected at least one card, found {count}.");
    }

    [Then("I should see the not found page")]
    public async Task ThenIShouldSeeTheNotFoundPage()
    {
        await Page.WaitForSelectorAsync("h3");
        Assert.Contains("Not Found", await Page.Locator("h3").First.InnerTextAsync());
    }

    [Then("the breadcrumb root should not be \"(.*)\"")]
    public async Task ThenTheBreadcrumbRootShouldNotBe(string label)
    {
        await Page.WaitForSelectorAsync(".breadcrumb");
        // The root crumb is the first link in the breadcrumb list.
        var root = await Page.Locator(".breadcrumb__list a").First.InnerTextAsync();
        Assert.NotEqual(label, root.Trim());
    }

    [Then("the page should show a breadcrumb")]
    public async Task ThenThePageShouldShowABreadcrumb()
    {
        Assert.True(await Page.Locator(".breadcrumb").IsVisibleAsync());
    }

    [Then("the breadcrumb should contain at least (.*) steps")]
    public async Task ThenTheBreadcrumbShouldContainAtLeastSteps(int minimum)
    {
        var count = await CountBreadcrumbStepsAsync();
        Assert.True(count >= minimum, $"Expected at least {minimum} breadcrumb steps, found {count}.");
    }

    [Then("the breadcrumb should contain (.*) step")]
    public async Task ThenTheBreadcrumbShouldContainStep(int expected)
    {
        var count = await CountBreadcrumbStepsAsync();
        Assert.Equal(expected, count);
    }

    // A "step" is a visited node: an ancestor crumb link plus the current node marker.
    // The static "Home" link and the "/" separators are intentionally excluded.
    private async Task<int> CountBreadcrumbStepsAsync()
    {
        var crumbLinks = await Page.Locator(".breadcrumb__link").CountAsync();
        var current = await Page.Locator(".breadcrumb__current").CountAsync();
        return crumbLinks + current;
    }
}
