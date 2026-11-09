using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

// Broad regression guards: every page loads without console errors or failing asset requests (including
// scoped CSS and Razor class library `_content/` assets), scoped styles still apply, and user settings persist.
[Binding]
public sealed class SmokeSteps(PlaywrightContext context, Reqnroll.UnitTestProvider.IUnitTestRuntimeProvider runtime)
{
    private readonly List<string> _problems = [];

    private IPage Page => context.Page;

    [Given("I am watching for page errors")]
    public void GivenIAmWatchingForPageErrors()
    {
        Page.Console += (_, message) =>
        {
            if (message.Type == "error" && !IsDevNoise(message.Text))
            {
                _problems.Add($"console error: {message.Text}");
            }
        };
        Page.PageError += (_, error) => _problems.Add($"page error: {error}");
        Page.Response += (_, response) =>
        {
            if (response.Status >= 400 && response.Url.StartsWith(context.BaseUrl, StringComparison.OrdinalIgnoreCase) && !IsDevNoise(response.Url))
            {
                _problems.Add($"HTTP {response.Status}: {response.Url}");
            }
        };
        Page.RequestFailed += (_, request) =>
        {
            if (request.Url.StartsWith(context.BaseUrl, StringComparison.OrdinalIgnoreCase)
                && !IsDevNoise(request.Url)
                && request.Failure?.Contains("ERR_ABORTED", StringComparison.OrdinalIgnoreCase) != true)
            {
                _problems.Add($"request failed ({request.Failure}): {request.Url}");
            }
        };
    }

    // Requests and messages injected by the dev server (hot reload, browser refresh) are not part of the app.
    private static bool IsDevNoise(string text) =>
        text.Contains("_framework/blazor-hotreload", StringComparison.OrdinalIgnoreCase)
        || text.Contains("aspnetcore-browser-refresh", StringComparison.OrdinalIgnoreCase)
        || text.Contains("_vs/browserLink", StringComparison.OrdinalIgnoreCase)
        || text.Contains("hot reload", StringComparison.OrdinalIgnoreCase);

    [When("I visit {string}")]
    public async Task WhenIVisit(string path)
    {
        await Page.GotoAsync($"{context.BaseUrl}/{path}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync("main");
    }

    [Then("no page errors should have occurred")]
    public void ThenNoPageErrorsShouldHaveOccurred()
    {
        Assert.True(_problems.Count == 0, string.Join(Environment.NewLine, _problems));
    }

    [Then("the largest text size button should use its scoped style")]
    public async Task ThenTheLargestTextSizeButtonShouldUseItsScopedStyle()
    {
        var button = Page.Locator(".quick-menu__section--textsize .textsize-picker__btn--largest");
        await button.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        // The scoped style sets 1.4rem, so compare against the root font size (which the text size setting changes).
        var ratio = 0d;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            ratio = await button.EvaluateAsync<double>(
                "el => parseFloat(getComputedStyle(el).fontSize) / parseFloat(getComputedStyle(document.documentElement).fontSize)");
            if (Math.Abs(ratio - 1.4) < 0.01)
            {
                break;
            }

            await Page.WaitForTimeoutAsync(250);
        }

        Assert.True(Math.Abs(ratio - 1.4) < 0.01, $"Expected the largest text size button to be 1.4rem but was {ratio}rem.");
    }

    [When("I choose the {string} text size")]
    public async Task WhenIChooseTheTextSize(string size)
    {
        var button = Page.Locator($".quick-menu__section--textsize .textsize-picker__btn--{size}");
        await button.ClickAsync();
        await Assertions.Expect(button).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [Then("the text size should be {string}")]
    public async Task ThenTheTextSizeShouldBe(string size)
    {
        await Assertions.Expect(Page.Locator("html")).ToHaveAttributeAsync("data-textsize", size);
    }

    [Then("I should see the heading {string}")]
    public async Task ThenIShouldSeeTheHeading(string heading)
    {
        await Assertions.Expect(Page.Locator("h1").Filter(new LocatorFilterOptions { HasText = heading }).First).ToBeVisibleAsync();
    }

    [Then("the service worker files should be published")]
    public async Task ThenTheServiceWorkerFilesShouldBePublished()
    {
        var probe = await Page.APIRequest.GetAsync($"{context.BaseUrl}/respondeo-deeplink-probe-404");
        if (probe.Status == 200)
        {
            runtime.TestIgnore("Service worker checks need the published site served by serve.py.");
            return;
        }

        foreach (var file in new[] { "service-worker.js", "service-worker-assets.js", "manifest.webmanifest" })
        {
            var response = await Page.APIRequest.GetAsync($"{context.BaseUrl}/{file}");
            Assert.True(response.Status == 200, $"{file} returned {response.Status}");
        }
    }
}
