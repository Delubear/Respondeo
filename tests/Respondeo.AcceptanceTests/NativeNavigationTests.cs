using Microsoft.Playwright;
using Xunit;

namespace Respondeo.AcceptanceTests;

public sealed class NativeNavigationTests : IAsyncLifetime
{
    private IPlaywright _playwright = default!;
    private IBrowser _browser = default!;
    private IPage _page = default!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync();
        _page = await _browser.NewPageAsync();
        await _page.RouteAsync("https://respondeo.test/**", route => route.FulfillAsync(new RouteFulfillOptions { Body = "" }));
        await _page.GotoAsync("https://respondeo.test/");
        await _page.SetContentAsync("""
            <html data-host="native">
            <head><base href="https://respondeo.test/" /></head>
            <body style="margin:0">
                <div class="page">
                    <header style="height:200px">Masthead</header>
                    <main id="content" class="content" tabindex="-1" style="min-height:3000px">
                        <div class="content__inner"><h1 style="margin:0">Destination</h1></div>
                    </main>
                    <footer>Colophon</footer>
                </div>
            </body>
            </html>
            """);
        var root = FindRepositoryRoot();
        await _page.AddStyleTagAsync(new PageAddStyleTagOptions
        {
            Path = Path.Combine(root, "src", "Respondeo.UI", "wwwroot", "css", "shell.css")
        });
        await _page.AddScriptTagAsync(new PageAddScriptTagOptions
        {
            Path = Path.Combine(root, "src", "Respondeo.UI", "wwwroot", "js", "site.js")
        });
    }

    [Fact]
    public async Task NativeRoute_HidesTheScrollingShellUntilTopLanding()
    {
        await _page.EvaluateAsync("window.respondeoScroll.beginNavigation('why-god')");

        await Assertions.Expect(_page.Locator("#content")).ToHaveCSSAsync("visibility", "hidden");
        await Assertions.Expect(_page.Locator("header")).ToHaveCSSAsync("visibility", "hidden");
        await Assertions.Expect(_page.Locator("footer")).ToHaveCSSAsync("visibility", "hidden");
        Assert.Equal(3000, await _page.Locator("#content").EvaluateAsync<int>("el => el.offsetHeight"));

        await _page.EvaluateAsync("window.respondeoScroll.land('top', false, false)");

        await Assertions.Expect(_page.Locator("#content")).ToHaveCSSAsync("visibility", "visible");
        await Assertions.Expect(_page.Locator("header")).ToBeVisibleAsync();
        Assert.Equal(0, await _page.EvaluateAsync<int>("window.scrollY"));
        await Assertions.Expect(_page.Locator("#content")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Back_MasksBeforeRouteHandlersAndNeverPaintsTheMastheadWhileRestoring()
    {
        await _page.EvaluateAsync("history.pushState({}, '', 'discover/miracles'); history.pushState({}, '', 'discover/miracles/fixture')");
        await _page.EvaluateAsync("window.respondeoScroll.beginNavigation('discover/miracles/fixture'); window.respondeoScroll.land('content', false, false)");
        await Assertions.Expect(_page.Locator("#content")).ToHaveCSSAsync("visibility", "visible");
        await _page.EvaluateAsync("window.scrollTo({ top: 1000, behavior: 'instant' })");
        await _page.EvaluateAsync("""
            () => {
                window.backPaints = [];
                window.addEventListener('popstate', () => {
                    window.maskedBeforeRoute = document.documentElement.hasAttribute('data-navigation-pending');
                    document.querySelector('#content').style.minHeight = '100px';
                    function sample() {
                        window.backPaints.push({
                            y: window.scrollY,
                            header: getComputedStyle(document.querySelector('header')).visibility,
                            content: getComputedStyle(document.querySelector('#content')).visibility
                        });
                        if (document.documentElement.hasAttribute('data-navigation-pending')) requestAnimationFrame(sample);
                    }
                    requestAnimationFrame(sample);
                }, { once: true });
                history.back();
            }
            """);
        await _page.WaitForFunctionAsync("() => window.maskedBeforeRoute !== undefined");
        Assert.True(await _page.EvaluateAsync<bool>("window.maskedBeforeRoute"));
        await Assertions.Expect(_page.Locator("header")).ToHaveCSSAsync("visibility", "hidden");

        await _page.EvaluateAsync("""
            () => {
                window.respondeoScroll.beginNavigation('discover/miracles');
                window.respondeoScroll.restore(1000);
                window.respondeoScroll.land('content', false, false);
                document.querySelector('#content').style.minHeight = '3000px';
            }
            """);

        await Assertions.Expect(_page.Locator("#content")).ToHaveCSSAsync("visibility", "visible");
        Assert.Equal(1000, await _page.EvaluateAsync<int>("window.scrollY"));
        Assert.True(await _page.EvaluateAsync<bool>("window.backPaints.length > 0 && window.backPaints.every(p => p.y >= 999 || (p.header === 'hidden' && p.content === 'hidden'))"));
    }

    [Theory]
    [InlineData("discover/miracles", "native", "", false, false, true)]
    [InlineData("discover/miracles", "native", "_self", false, false, true)]
    [InlineData("discover/miracles", "web", "", false, false, false)]
    [InlineData("#content", "native", "", false, false, false)]
    [InlineData("?q=faith", "native", "", false, false, false)]
    [InlineData("https://other.test/miracles", "native", "", false, false, false)]
    [InlineData("discover/miracles", "native", "_blank", false, false, false)]
    [InlineData("discover/miracles", "native", "", true, false, false)]
    [InlineData("discover/miracles", "native", "", false, true, false)]
    public async Task LinkCapture_MasksOnlyNativeRouteChangesBeforeBlazor(string href, string host, string target, bool download, bool modified, bool expected)
    {
        var masked = await _page.EvaluateAsync<bool>("""
            ({ href, host, target, download, modified }) => {
                document.documentElement.dataset.host = host;
                const link = document.createElement('a');
                link.href = href;
                link.target = target;
                if (download) link.setAttribute('download', '');
                document.body.appendChild(link);
                let masked = false;
                link.addEventListener('click', event => {
                    masked = document.documentElement.hasAttribute('data-navigation-pending');
                    event.preventDefault();
                });
                link.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, button: 0, ctrlKey: modified }));
                return masked;
            }
            """, new { href, host, target, download, modified });

        Assert.Equal(expected, masked);
    }

    [Theory]
    [InlineData("native", false, "none")]
    [InlineData("web", false, "fade-enter")]
    [InlineData("web", true, "none")]
    public async Task PageEntryMotion_IsDisabledOnlyForNativeOrReducedMotion(string host, bool reduced, string animation)
    {
        await _page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = reduced ? ReducedMotion.Reduce : ReducedMotion.NoPreference });
        await _page.EvaluateAsync("host => document.documentElement.dataset.host = host", host);
        await _page.AddStyleTagAsync(new PageAddStyleTagOptions
        {
            Path = Path.Combine(FindRepositoryRoot(), "src", "Respondeo.UI", "wwwroot", "css", "content.css")
        });
        await _page.EvaluateAsync("""
            () => {
                document.documentElement.style.setProperty('--dur-4', '10s');
                document.documentElement.style.setProperty('--ease-page', 'linear');
                document.querySelector('.content__inner').classList.add('fade-enter');
            }
            """);

        await Assertions.Expect(_page.Locator(".content__inner")).ToHaveCSSAsync("animation-name", animation);
        if (animation == "none")
        {
            await Assertions.Expect(_page.Locator(".content__inner")).ToHaveCSSAsync("opacity", "1");
            await Assertions.Expect(_page.Locator(".content__inner")).ToHaveCSSAsync("transform", "none");
        }
    }

    [Fact]
    public async Task ContentLanding_RevealsAtContentTop()
    {
        await _page.EvaluateAsync("""
            () => {
                window.respondeoScroll.beginNavigation('why-god');
                window.respondeoScroll.land('content', false, false);
            }
            """);

        await Assertions.Expect(_page.Locator("#content")).ToHaveCSSAsync("visibility", "visible");
        Assert.Equal(200, await _page.EvaluateAsync<int>("window.scrollY"));
    }

    [Fact]
    public async Task Restore_WaitsForReachablePositionAndWinsOverDefault()
    {
        await _page.EvaluateAsync("""
            () => {
                document.querySelector('#content').style.minHeight = '100px';
                window.respondeoScroll.beginNavigation('summa');
                window.respondeoScroll.restore(1000);
                window.respondeoScroll.land('content', false, false);
            }
            """);
        await Assertions.Expect(_page.Locator("#content")).ToHaveCSSAsync("visibility", "hidden");

        await _page.EvaluateAsync("document.querySelector('#content').style.minHeight = '3000px'");

        await Assertions.Expect(_page.Locator("#content")).ToHaveCSSAsync("visibility", "visible");
        Assert.Equal(1000, await _page.EvaluateAsync<int>("window.scrollY"));
    }

    [Theory]
    [InlineData("#content", "native")]
    [InlineData("?q=faith", "native")]
    [InlineData("why-god", "web")]
    public async Task SamePageOrWebNavigation_DoesNotMask(string path, string host)
    {
        await _page.EvaluateAsync("host => document.documentElement.dataset.host = host", host);
        await _page.EvaluateAsync("path => window.respondeoScroll.beginNavigation(path)", path);

        Assert.False(await _page.Locator("html").EvaluateAsync<bool>("el => el.hasAttribute('data-navigation-pending')"));
        await Assertions.Expect(_page.Locator("#content")).ToHaveCSSAsync("visibility", "visible");
    }

    [Fact]
    public async Task MissingLanding_RevealsContentAndLogsWarningWithinTimeout()
    {
        var warning = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _page.Console += (_, message) =>
        {
            if (message.Type == "warning")
            {
                warning.TrySetResult(message.Text);
            }
        };
        await _page.EvaluateAsync("window.respondeoScroll.beginNavigation('why-god')");
        await Assertions.Expect(_page.Locator("#content")).ToHaveCSSAsync("visibility", "hidden");

        await Assertions.Expect(_page.Locator("#content")).ToHaveCSSAsync("visibility", "visible",
            new LocatorAssertionsToHaveCSSOptions { Timeout = 4000 });
        Assert.Contains("within 3 seconds", await warning.Task.WaitAsync(TimeSpan.FromSeconds(1)));
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }
        _playwright?.Dispose();
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
        throw new DirectoryNotFoundException("Cannot locate Respondeo.slnx for the browser helper tests.");
    }
}
