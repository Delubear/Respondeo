using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

[Binding]
public sealed class SummaSteps(PlaywrightContext context)
{
    // How far down to scroll before navigating away. The restore assertion only checks that we land
    // well below the top (not an exact pixel), so the test proves "scroll was restored" without being
    // brittle about async content height shifting the clamp by a few pixels.
    private const int ScrollTarget = 1200;
    private const int RestoredScrollFloor = 300;

    private IPage Page => context.Page;

    [Given("I open the Summa browse page")]
    [When("I open the Summa browse page")]
    public async Task OpenTheSummaBrowsePage()
    {
        // Blazor WebAssembly boots after the load event, so wait for the network to settle and for the
        // browse accordion to render before interacting.
        await Page.GotoAsync($"{context.BaseUrl}/summa", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync("details.summa__part");
    }

    [When("I expand the first part")]
    public async Task ExpandTheFirstPart()
    {
        var firstPart = Page.Locator("details.summa__part").First;
        if (await firstPart.GetAttributeAsync("open") is null)
        {
            await firstPart.Locator("summary.summa__part-summary").ClickAsync();
        }

        await Assertions.Expect(firstPart).ToHaveAttributeAsync("open", string.Empty);
    }

    [When("I scroll the browse page down")]
    public async Task ScrollTheBrowsePageDown()
    {
        await Page.EvaluateAsync($"window.scrollTo(0, {ScrollTarget})");
        // Confirm the scroll took effect before navigating away.
        await Page.WaitForFunctionAsync($"() => window.scrollY >= {RestoredScrollFloor}");
    }

    [When("I open the first question")]
    public async Task OpenTheFirstQuestion()
    {
        // In the first part the questions may sit inside a nested, still-collapsed treatise <details>,
        // so their links exist in the DOM but are not visible. Expand the first treatise if present,
        // then click the first VISIBLE question link.
        var firstTreatise = Page.Locator("details.summa__treatise").First;
        if (await firstTreatise.CountAsync() > 0 && await firstTreatise.GetAttributeAsync("open") is null)
        {
            await firstTreatise.Locator("summary.summa__treatise-summary").ClickAsync();
        }

        var question = Page.Locator("a.summa__question-link:visible").First;
        await question.ClickAsync();
        await Page.WaitForSelectorAsync("nav.breadcrumb");
    }

    [When("I search the Summa for \"(.*)\"")]
    public async Task SearchTheSummaFor(string term)
    {
        await Page.Locator("#summa-search").FillAsync(term);
        await Page.WaitForSelectorAsync("a.summa__match-link");
    }

    [When("I open the first search result")]
    public async Task OpenTheFirstSearchResult()
    {
        await Page.Locator("a.summa__match-link").First.ClickAsync();
        await Page.WaitForSelectorAsync("nav.breadcrumb");
    }

    [When("I navigate to the home page")]
    public async Task NavigateToTheHomePage()
    {
        await Page.GotoAsync($"{context.BaseUrl}/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync(".reel__q");
    }

    [When("I press the browser back button")]
    public async Task PressTheBrowserBackButton()
    {
        await Page.GoBackAsync(new PageGoBackOptions { WaitUntil = WaitUntilState.NetworkIdle });
        // The search box renders in both browse and search modes, so it is the reliable signal that the
        // Summa page itself is back. The browse list (.summa__part) is absent while a search is active,
        // so we must not wait on it here.
        await Page.WaitForSelectorAsync("#summa-search");
    }

    [Then("the first part should be expanded")]
    public async Task TheFirstPartShouldBeExpanded()
    {
        await Assertions.Expect(Page.Locator("details.summa__part").First).ToHaveAttributeAsync("open", string.Empty);
    }

    [Then("the browse page should be scrolled down")]
    public async Task TheBrowsePageShouldBeScrolledDown()
    {
        // The restore helper re-applies the saved offset across a settle window (it must outlast Blazor's
        // FocusOnNavigate reset), so poll until the window lands below the floor rather than reading a
        // single instantaneous value that could race the restore.
        await Page.WaitForFunctionAsync(
            $"() => window.scrollY >= {RestoredScrollFloor}",
            null,
            new PageWaitForFunctionOptions { Timeout = 15000, PollingInterval = 100 });
    }

    [Then("the Summa search box should contain \"(.*)\"")]
    public async Task TheSummaSearchBoxShouldContain(string term)
    {
        await Assertions.Expect(Page.Locator("#summa-search")).ToHaveValueAsync(term);
    }

    [Then("search results should be shown")]
    public async Task SearchResultsShouldBeShown()
    {
        await Page.WaitForSelectorAsync("a.summa__match-link");
        Assert.True(await Page.Locator("a.summa__match-link").CountAsync() > 0);
    }

    [Then("the Summa search box should be empty")]
    public async Task TheSummaSearchBoxShouldBeEmpty()
    {
        await Assertions.Expect(Page.Locator("#summa-search")).ToHaveValueAsync(string.Empty);
    }
}
