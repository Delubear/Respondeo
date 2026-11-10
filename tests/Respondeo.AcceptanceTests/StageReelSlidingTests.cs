using Microsoft.Playwright;
using Xunit;

namespace Respondeo.AcceptanceTests;

public sealed class StageReelSlidingTests
{
    [Theory]
    [InlineData(390, false)]
    [InlineData(800, false)]
    [InlineData(1280, false)]
    [InlineData(390, true)]
    public async Task Reel_SlidesWithoutCardTransformsOrAnimations(int width, bool reducedMotion)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = 900 },
            ReducedMotion = reducedMotion ? ReducedMotion.Reduce : ReducedMotion.NoPreference
        });
        var root = FindRepositoryRoot();
        var module = await File.ReadAllTextAsync(Path.Combine(root, "src", "Respondeo.UI", "wwwroot", "js", "reel.js"));
        await page.RouteAsync("https://respondeo.test/**", route => route.FulfillAsync(new RouteFulfillOptions
        {
            ContentType = "text/javascript",
            Body = module
        }));
        await page.GotoAsync("https://respondeo.test/");
        await page.SetContentAsync("""
            <style>* { box-sizing: border-box; } body { margin: 0; } :root { --w-wide: 100%; }</style>
            <div class="reel-wrap">
                <div class="reel" tabindex="0">
                    <div class="reel__step"><a class="reel__q" href="why-god">First question</a></div>
                    <div class="reel__step"><a class="reel__q" href="which-god">Second question</a></div>
                    <div class="reel__step"><a class="reel__q" href="why-jesus">Third question</a></div>
                </div>
                <button class="reel__hint reel__hint--prev"><span class="reel__chevron"></span></button>
                <button class="reel__hint reel__hint--next"><span class="reel__chevron"></span></button>
                <div class="reel-dots"></div>
            </div>
            """);
        var css = await File.ReadAllTextAsync(Path.Combine(root, "src", "Respondeo.UI", "Components", "Inquiry", "StageReel.razor.css"));
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = css.Replace("::deep", "") });
        await Assertions.Expect(page.Locator(".reel__step").Nth(0)).ToHaveCSSAsync("opacity", "1");
        await Assertions.Expect(page.Locator(".reel__step").Nth(1)).ToHaveCSSAsync("opacity", "0");
        await Assertions.Expect(page.Locator(".reel__step").Nth(2)).ToHaveCSSAsync("opacity", "0");
        await page.EvaluateAsync("""
            async () => {
                window.reelModule = await import('https://respondeo.test/reel.js');
                const reel = document.querySelector('.reel');
                window.reelModule.init(reel);
                document.querySelector('.reel__hint--prev').onclick = () => window.reelModule.scrollByStep(reel, -1);
                document.querySelector('.reel__hint--next').onclick = () => window.reelModule.scrollByStep(reel, 1);
            }
            """);
        await AssertCenteredAsync(page, 0);
        await Assertions.Expect(page.Locator(".reel")).ToHaveCSSAsync("perspective", "none");
        await Assertions.Expect(page.Locator(".reel")).ToHaveCSSAsync("contain", "none");
        Assert.Equal(0, await page.EvaluateAsync<int>("document.querySelector('.reel-wrap').getAnimations({ subtree: true }).length"));
        await page.AddStyleTagAsync(new PageAddStyleTagOptions
        {
            Content = ":root { --dur-3: 0.3s; --ease-page: cubic-bezier(0.22, 0.61, 0.36, 1); }"
        });
        await Assertions.Expect(page.Locator(".reel__step").Nth(0))
            .ToHaveCSSAsync("transition-duration", reducedMotion ? "0s" : "0.3s");
        if (!reducedMotion)
        {
            await Assertions.Expect(page.Locator(".reel__step").Nth(0)).ToHaveCSSAsync("transition-property", "opacity");
        }

        await page.Locator(".reel__hint--next").ClickAsync();
        await AssertCenteredAsync(page, 1);
        var forward = await page.Locator(".reel").EvaluateAsync<double>("el => el.scrollLeft");
        Assert.True(forward > 0);

        await page.Locator(".reel").PressAsync("End");
        await AssertCenteredAsync(page, 2);
        await Assertions.Expect(page.Locator(".reel__hint--next")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-hidden"));

        await page.Locator(".reel-dots__dot").Nth(0).ClickAsync();
        await AssertCenteredAsync(page, 0);
        Assert.True(await page.Locator(".reel").EvaluateAsync<double>("el => el.scrollLeft") < forward);

        await page.Locator(".reel").EvaluateAsync("""
            reel => {
                const start = new Touch({ identifier: 1, target: reel, clientX: 250, clientY: 100 });
                const end = new Touch({ identifier: 1, target: reel, clientX: 150, clientY: 100 });
                reel.dispatchEvent(new TouchEvent('touchstart', { touches: [start] }));
                reel.dispatchEvent(new TouchEvent('touchend', { changedTouches: [end] }));
            }
            """);
        await AssertCenteredAsync(page, 1);

        foreach (var step in await page.Locator(".reel__step").AllAsync())
        {
            await Assertions.Expect(step).ToHaveCSSAsync("transform", "none");
            await Assertions.Expect(step).ToHaveCSSAsync("animation-name", "none");
        }
        await page.EvaluateAsync("window.reelModule.dispose(document.querySelector('.reel'))");
        await Assertions.Expect(page.Locator(".reel-dots__dot")).ToHaveCountAsync(0);
    }

    private static async Task AssertCenteredAsync(IPage page, int index)
    {
        await page.WaitForFunctionAsync("""
            index => {
                const reel = document.querySelector('.reel');
                const step = reel.querySelectorAll('.reel__step')[index];
                const rect = step.getBoundingClientRect();
                const viewport = reel.getBoundingClientRect();
                return step.classList.contains('is-centered')
                    && Math.abs(rect.left + rect.width / 2 - viewport.left - viewport.width / 2) < 2;
            }
            """, index);
        await Assertions.Expect(page.Locator(".reel-dots__dot").Nth(index)).ToHaveAttributeAsync("aria-current", "true");
        var steps = await page.Locator(".reel__step").AllAsync();
        for (var i = 0; i < steps.Count; i++)
        {
            await Assertions.Expect(steps[i]).ToHaveCSSAsync("opacity", i == index ? "1" : "0");
        }
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Respondeo.slnx")))
            {
                return directory.FullName;
            }
        }
        throw new DirectoryNotFoundException("Cannot locate Respondeo.slnx for the reel tests.");
    }
}
