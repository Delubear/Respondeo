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

    [Then("I should see at least one stage card")]
    public async Task ThenIShouldSeeAtLeastOneStageCard()
    {
        var count = await Page.Locator(".card").CountAsync();
        Assert.True(count >= 1, $"Expected at least one card, found {count}.");
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
