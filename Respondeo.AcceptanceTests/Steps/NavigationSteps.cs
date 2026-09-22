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
        await Page.WaitForSelectorAsync(".stage-card");
    }

    [When("I choose the first stage card")]
    public async Task WhenIChooseTheFirstStageCard()
    {
        // The home reel renders the journey with Stage 1 at the BOTTOM (climbing upward) and
        // scrolls to it on init. Stage 1 is the LAST `.reel__step`, so we wait for the bottom
        // card specifically to become centred before interacting.
        var stageOne = Page.Locator(".reel__step:last-child.is-centered");
        await stageOne.WaitForAsync();
        var card = stageOne.Locator(".stage-card");

        // Clicking a CENTRED card follows its link, but Playwright's pre-click auto-scroll (or a
        // snap-settle) can momentarily un-centre the card, so the reel's capture-phase handler
        // treats the click as "re-centre this neighbour" and swallows it instead of navigating.
        // That leaves the card centred, so a retry navigates. Loop until the URL actually changes.
        var href = await card.GetAttributeAsync("href");
        for (var attempt = 0; attempt < 3; attempt++)
        {
            await card.ClickAsync();
            try
            {
                await Page.WaitForURLAsync($"**/{href}", new PageWaitForURLOptions { Timeout = 5000 });
                break;
            }
            catch (TimeoutException)
            {
                // Click was intercepted and the card re-centred; retry now that it is centred.
            }
        }

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
        await Page.WaitForSelectorAsync(".stage-card");
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
        // The home reel renders .stage-card; stage pages render their branch .card list.
        // Either satisfies "at least one card to move forward from here".
        var count = await Page.Locator(".stage-card, .card").CountAsync();
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

    [When("I press the \"keep climbing\" control")]
    public async Task WhenIPressTheKeepClimbingControl()
    {
        await ReelHint("keep climbing").ClickAsync();
        // The reel scrolls smoothly; wait for the "earlier steps" control to become active.
        await Page.WaitForSelectorAsync(".reel__hint--down:not(.is-hidden)");
    }

    [Then("the \"(.*)\" control should be visible")]
    public async Task ThenTheControlShouldBeVisible(string control)
    {
        await Assertions.Expect(ReelHint(control)).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bis-hidden\b"));
    }

    [Then("the \"(.*)\" control should be hidden")]
    public async Task ThenTheControlShouldBeHidden(string control)
    {
        await Assertions.Expect(ReelHint(control)).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bis-hidden\b"));
    }

    // Maps the human-readable control name to its reel hint element.
    private ILocator ReelHint(string control) => control.Trim().ToLowerInvariant() switch
    {
        "keep climbing" => Page.Locator(".reel__hint--up"),
        "earlier steps" => Page.Locator(".reel__hint--down"),
        _ => throw new ArgumentOutOfRangeException(nameof(control), control, "Unknown reel control."),
    };

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
