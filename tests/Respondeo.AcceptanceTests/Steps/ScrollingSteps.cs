using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

[Binding]
public sealed class ScrollingSteps(PlaywrightContext context)
{
    // Landing is deferred until content renders and may run a smooth scroll, so poll rather than read once.
    private const int TopTolerance = 5;
    private const float LandingTimeout = 10_000;

    private IPage Page => context.Page;

    [Then(@"article (\d+) should be in the viewport")]
    public async Task ArticleShouldBeInTheViewport(int articleNumber)
    {
        await Assertions.Expect(Page.Locator($"#article-{articleNumber}"))
            .ToBeInViewportAsync(new LocatorAssertionsToBeInViewportOptions { Timeout = LandingTimeout });
    }

    [Then("the page should be scrolled below the top")]
    public async Task PageShouldBeScrolledBelowTheTop()
    {
        await Page.WaitForFunctionAsync(
            $"() => window.scrollY > {TopTolerance}",
            null,
            new PageWaitForFunctionOptions { Timeout = LandingTimeout });
    }

    [Then("the page breadcrumb should be in the viewport")]
    public async Task PageBreadcrumbShouldBeInTheViewport()
    {
        await Assertions.Expect(Page.Locator("nav.breadcrumb"))
            .ToBeInViewportAsync(new LocatorAssertionsToBeInViewportOptions { Timeout = LandingTimeout });
    }
}
