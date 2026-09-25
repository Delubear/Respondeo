using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

[Binding]
public sealed class SummaSteps(PlaywrightContext context)
{
    // How far down to scroll before leaving, and the minimum restored offset we accept back. The
    // threshold is generous so the test asserts "scroll was restored, not reset to the top" without
    // being brittle about exact pixels (async content height can shift the clamp slightly).
    private const int ScrollTarget = 1200;
    private const int RestoredScrollFloor = 400;

    private IPage Page => context.Page;

    [Given("I open the Summa browse page")]
    [When("I open the Summa browse page")]
    public async Task GivenIOpenTheSummaBrowsePage()
    {
        // Blazor WebAssembly loads its runtime after the load event, so wait for the network to
        // settle and for the browse accordion to render before interacting.
        await Page.GotoAsync($"{context.BaseUrl}/summa", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync(".summa__part");
    }

    [When("I expand the first part")]
    public async Task WhenIExpandTheFirstPart()
    {
        var firstPart = Page.Locator("details.summa__part").First;
        // Only expand if it isn't already open (a part-scoped seed could have opened it).
        if (await firstPart.GetAttributeAsync("open") is null)
        {
            await firstPart.Locator("summary.summa__part-summary").ClickAsync();
        }

        await Assertions.Expect(firstPart).ToHaveAttributeAsync("open", string.Empty);
    }

    [When("I scroll the browse page down")]
    public async Task WhenIScrollTheBrowsePageDown()
    {
        await Page.EvaluateAsync($"window.scrollTo(0, {ScrollTarget})");
        // Confirm the scroll actually took effect before we navigate away.
        await Page.WaitForFunctionAsync($"() => window.scrollY >= {RestoredScrollFloor}");
    }

    [When("I open the first question")]
    public async Task WhenIOpenTheFirstQuestion()
    {
        // In the first part the questions may sit inside a nested, still-collapsed treatise
        // <details>, so their links exist in the DOM but are not visible. Expand the first treatise
        // if present, then click the first VISIBLE question link.
        var firstTreatise = Page.Locator("details.summa__treatise").First;
        if (await firstTreatise.CountAsync() > 0 && await firstTreatise.GetAttributeAsync("open") is null)
        {
            await firstTreatise.Locator("summary.summa__treatise-summary").ClickAsync();
        }

        var visibleQuestion = Page.Locator("a.summa__question-link:visible").First;
        await visibleQuestion.ClickAsync();
        // Landed on a question page: its breadcrumb marks the browse list is gone.
        await Page.WaitForSelectorAsync(".breadcrumb");
    }

    [When("I search the Summa for \"(.*)\"")]
    public async Task WhenISearchTheSummaFor(string term)
    {
        await Page.Locator("#summa-search").FillAsync(term);
        await Page.WaitForSelectorAsync("a.summa__match-link");
    }

    [When("I open the first search result")]
    public async Task WhenIOpenTheFirstSearchResult()
    {
        await Page.Locator("a.summa__match-link").First.ClickAsync();
        await Page.WaitForSelectorAsync(".breadcrumb");
    }

    [When("I navigate to the home page")]
    public async Task WhenINavigateToTheHomePage()
    {
        await Page.GotoAsync($"{context.BaseUrl}/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync(".stage-card");
    }

    [When("I press the browser back button")]
    public async Task WhenIPressTheBrowserBackButton()
    {
        await Page.GoBackAsync(new PageGoBackOptions { WaitUntil = WaitUntilState.NetworkIdle });
        // Wait for the Summa page itself to be back (the search box renders in both browse and search
        // modes); the browse list (.summa__part) is absent when a search is active, so we must not
        // wait on it here.
        await Page.WaitForSelectorAsync("#summa-search");
    }

    [Then("the first part should be expanded")]
    public async Task ThenTheFirstPartShouldBeExpanded()
    {
        await Assertions.Expect(Page.Locator("details.summa__part").First).ToHaveAttributeAsync("open", string.Empty);
    }

    [Then("the browse page should be scrolled down")]
    public async Task ThenTheBrowsePageShouldBeScrolledDown()
    {
        // The restore poll re-applies across a settle window; wait for it to land above the floor
        // rather than reading a single instantaneous value that might race the restore.
        await Page.WaitForFunctionAsync($"() => window.scrollY >= {RestoredScrollFloor}");
    }

    [Then("the Summa search box should contain \"(.*)\"")]
    public async Task ThenTheSummaSearchBoxShouldContain(string term)
    {
        await Assertions.Expect(Page.Locator("#summa-search")).ToHaveValueAsync(term);
    }

    [Then("search results should be shown")]
    public async Task ThenSearchResultsShouldBeShown()
    {
        await Page.WaitForSelectorAsync("a.summa__match-link");
        Assert.True(await Page.Locator("a.summa__match-link").CountAsync() > 0);
    }

    [Then("the Summa search box should be empty")]
    public async Task ThenTheSummaSearchBoxShouldBeEmpty()
    {
        await Assertions.Expect(Page.Locator("#summa-search")).ToHaveValueAsync(string.Empty);
    }
}
