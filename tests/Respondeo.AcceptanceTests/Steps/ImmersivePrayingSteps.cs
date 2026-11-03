using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

/// <summary>
/// Steps for the immersive praying views shared by single prayers, devotions and the Liturgy of the Hours:
/// the pinned bottom bar, the chrome that steps aside on phones, and how Next/Back keep the current step in view.
/// Scroll assertions poll with WaitForFunction because the players glide with smooth scrolling.
/// </summary>
[Binding]
public sealed class ImmersivePrayingSteps(PlaywrightContext context)
{
    private const float PhoneWidth = 390;
    private const float PhoneHeight = 844;

    private IPage Page => context.Page;

    [Given("I am on a phone-sized screen")]
    public Task GivenIAmOnAPhoneSizedScreen() =>
        Page.SetViewportSizeAsync((int)PhoneWidth, (int)PhoneHeight);

    [Given("I open the \"(.*)\" hour")]
    public async Task GivenIOpenTheHour(string slug)
    {
        await Page.GotoAsync($"{context.BaseUrl}/discover/hours/{slug}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync(".playerbar");
    }

    // ---------- Prayer reader bar ----------

    [Then("the prayer reader bar should be visible")]
    public async Task ThenThePrayerReaderBarShouldBeVisible() =>
        Assert.True(await Page.Locator(".readerbar").IsVisibleAsync(), "Expected the prayer reader bar to be visible.");

    [Then("the prayer reader bar should link back to all prayers")]
    public async Task ThenThePrayerReaderBarShouldLinkBackToAllPrayers()
    {
        var leave = Page.Locator("a.readerbar__leave[href='discover/prayers']");
        Assert.True(await leave.IsVisibleAsync(), "Expected the reader bar to link back to all prayers.");
    }

    [When("I switch the prayer language")]
    public Task WhenISwitchThePrayerLanguage() => Page.Locator(".readerbar .devotion__lang-mini").ClickAsync();

    [Then("the prayer text should be in Latin")]
    public Task ThenThePrayerTextShouldBeInLatin() =>
        Page.WaitForFunctionAsync("() => document.querySelector('.prayer__text')?.innerText.includes('Sancta Mar')");

    [Then("the prayer text should be in English")]
    public Task ThenThePrayerTextShouldBeInEnglish() =>
        Page.WaitForFunctionAsync("() => document.querySelector('.prayer__text')?.innerText.includes('Hail Mary')");

    [When("I change the text size from the reader bar")]
    public Task WhenIChangeTheTextSizeFromTheReaderBar() => Page.Locator(".readerbar__size").ClickAsync();

    [Then("the document text size should not be the default")]
    public Task ThenTheDocumentTextSizeShouldNotBeTheDefault() =>
        Page.WaitForFunctionAsync("() => { const s = document.documentElement.getAttribute('data-textsize'); return s && s !== 'default'; }");

    [When("I leave the prayer from the reader bar")]
    public async Task WhenILeaveThePrayerFromTheReaderBar()
    {
        await Page.Locator("a.readerbar__leave").ClickAsync();
        await Page.WaitForURLAsync("**/discover/prayers");
    }

    [Then("I should be on the prayers browser")]
    public void ThenIShouldBeOnThePrayersBrowser() =>
        Assert.EndsWith("/discover/prayers", Page.Url.TrimEnd('/'));

    [When("I toggle the theme from the reader bar")]
    public Task WhenIToggleTheThemeFromTheReaderBar() => Page.Locator(".readerbar .theme-toggle").ClickAsync();

    [Then("every theme toggle should offer the light theme")]
    public Task ThenEveryThemeToggleShouldOfferTheLightTheme() =>
        Page.WaitForFunctionAsync(
            "() => { const t = [...document.querySelectorAll('.theme-toggle')];" +
            " return t.length > 1 && t.every(b => b.getAttribute('aria-label') === 'Switch to light theme'); }");

    [When("I open the prayer guide from the reader bar")]
    public async Task WhenIOpenThePrayerGuideFromTheReaderBar()
    {
        await Page.Locator(".readerbar__guide").ClickAsync();
        await Page.WaitForSelectorAsync(".dialog[open]");
    }

    // ---------- Immersive mode and chrome ----------

    [Then("the page should be in immersive mode")]
    public Task ThenThePageShouldBeInImmersiveMode() =>
        Page.WaitForFunctionAsync("() => document.body.classList.contains('is-immersive')");

    [Then("the page should not be in immersive mode")]
    public Task ThenThePageShouldNotBeInImmersiveMode() =>
        Page.WaitForFunctionAsync("() => !document.body.classList.contains('is-immersive')");

    [Then("the masthead should be hidden")]
    public Task ThenTheMastheadShouldBeHidden() => AssertHiddenAsync(".masthead");

    [Then("the floating guide orb should be hidden")]
    public Task ThenTheFloatingGuideOrbShouldBeHidden() => AssertHiddenAsync(".float-circle");

    [Then("the back-to-top arrow should be hidden")]
    public Task ThenTheBackToTopArrowShouldBeHidden() => AssertHiddenAsync(".back-to-top");

    // ---------- Player bar (devotions and hours) ----------

    [Then("the player bar should be visible")]
    public async Task ThenThePlayerBarShouldBeVisible() =>
        Assert.True(await Page.Locator(".playerbar").IsVisibleAsync(), "Expected the player bar to be visible.");

    [When("I press Next (\\d+) times in the player bar")]
    public async Task WhenIPressNextTimesInThePlayerBar(int times)
    {
        var next = Page.Locator(".playerbar__nav", new PageLocatorOptions { HasText = "Next" });
        for (var i = 0; i < times; i++)
        {
            await next.ClickAsync();
        }

        await WaitForScrollToSettleAsync();
    }

    [When("I press Back in the player bar")]
    public async Task WhenIPressBackInThePlayerBar()
    {
        await Page.Locator(".playerbar__nav", new PageLocatorOptions { HasText = "Back" }).ClickAsync();
        await WaitForScrollToSettleAsync();
    }

    [Then("the player bar should show (\\d+) completed steps?")]
    public Task ThenThePlayerBarShouldShowCompletedSteps(int completed) =>
        Page.WaitForFunctionAsync(
            $"() => /\\b{completed} of \\d+/.test(document.querySelector('.playerbar__count')?.innerText ?? '')");

    [Then("the page should be scrolled to the top")]
    public async Task ThenThePageShouldBeScrolledToTheTop()
    {
        await WaitForScrollToSettleAsync();
        await Page.WaitForFunctionAsync("() => window.scrollY < 4");
    }

    [Then("the current devotion step should sit above the player bar")]
    public Task ThenTheCurrentDevotionStepShouldSitAboveThePlayerBar() => AssertAboveBarAsync("devotion-current-step");

    [Then("the current devotion step should be below the top of the screen")]
    public Task ThenTheCurrentDevotionStepShouldBeBelowTheTopOfTheScreen() =>
        Page.WaitForFunctionAsync("() => (document.getElementById('devotion-current-step')?.getBoundingClientRect().top ?? -1) >= 0");

    [Then("the current hour step should start above the player bar")]
    public Task ThenTheCurrentHourStepShouldSitAboveThePlayerBar() =>
        // Hour steps (a whole psalm) are often taller than the screen; the player lands each step's start
        // at the top, so the start must be on screen and clear of the bar rather than the whole step.
        Page.WaitForFunctionAsync(
            "() => { const e = document.getElementById('hour-current-step'); const b = document.querySelector('.playerbar');" +
            " if (!e || !b) return false; const r = e.getBoundingClientRect();" +
            " return r.top >= 0 && r.top + 40 <= b.getBoundingClientRect().top; }");

    [When("I advance until the current hour step starts a new part")]
    public async Task WhenIAdvanceUntilTheCurrentHourStepStartsANewPart()
    {
        var next = Page.Locator(".playerbar__nav", new PageLocatorOptions { HasText = "Next" });

        // Step past the opening step first so the landing is a genuine part change, not the page's first banner.
        for (var i = 0; i < 40; i++)
        {
            await next.ClickAsync();
            await WaitForScrollToSettleAsync();

            var startsPart = await Page.EvaluateAsync<bool>(
                "() => document.getElementById('hour-current-step')?.previousElementSibling?.classList.contains('hourplayer__banner') ?? false");
            if (startsPart)
            {
                return;
            }
        }

        Assert.Fail("Never reached a step that opens a new part of the hour.");
    }

    [Then("that part's heading should be in the viewport")]
    public Task ThenThatPartsHeadingShouldBeInTheViewport() =>
        Page.WaitForFunctionAsync(
            "() => { const h = document.getElementById('hour-current-step')?.previousElementSibling" +
            "?.querySelector('.hourplayer__section-title'); if (!h) return false;" +
            " const r = h.getBoundingClientRect(); return r.top >= 0 && r.bottom <= window.innerHeight; }");

    // The element's bottom must clear the top edge of the pinned bar (and its top must be on screen).
    private Task AssertAboveBarAsync(string id) =>
        Page.WaitForFunctionAsync(
            $"() => {{ const e = document.getElementById('{id}'); const b = document.querySelector('.playerbar');" +
            " if (!e || !b) return false; const r = e.getBoundingClientRect();" +
            " return r.top >= 0 && r.bottom <= b.getBoundingClientRect().top; }");

    private async Task AssertHiddenAsync(string selector)
    {
        var elements = Page.Locator(selector);
        var count = await elements.CountAsync();
        for (var i = 0; i < count; i++)
        {
            Assert.False(await elements.Nth(i).IsVisibleAsync(), $"Expected '{selector}' to be hidden.");
        }
    }

    // Smooth scrolling can take a few hundred milliseconds; wait until scrollY has held still for several frames.
    private Task WaitForScrollToSettleAsync() =>
        Page.EvaluateAsync(
            "() => new Promise(resolve => { let last = -1, still = 0;" +
            " const tick = () => { if (window.scrollY === last) { if (++still >= 6) return resolve(); } else { still = 0; last = window.scrollY; }" +
            " requestAnimationFrame(tick); }; requestAnimationFrame(tick); })");
}
