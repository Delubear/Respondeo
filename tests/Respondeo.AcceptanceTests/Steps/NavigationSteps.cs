using Microsoft.Playwright;
using Reqnroll;
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
        await Page.GotoAsync(context.BaseUrl + "/");
        await Page.WaitForSelectorAsync(".card");
    }

    [When("I choose the first entry-point card")]
    public async Task WhenIChooseTheFirstEntryPointCard()
    {
        await Page.Locator(".card").First.ClickAsync();
        await Page.WaitForSelectorAsync(".breadcrumb");
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

    [Then("I should see at least one entry-point card")]
    public async Task ThenIShouldSeeAtLeastOneEntryPointCard()
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

    // A "step" is a visited node: crumb links to a node plus the current node marker.
    // The static "Start" link and the "/" separators are intentionally excluded.
    private async Task<int> CountBreadcrumbStepsAsync()
    {
        var crumbLinks = await Page.Locator(".breadcrumb a[href^='node']").CountAsync();
        var current = await Page.Locator(".breadcrumb__current").CountAsync();
        return crumbLinks + current;
    }
}
