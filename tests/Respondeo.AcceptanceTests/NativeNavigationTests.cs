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
                <header style="height:200px">Masthead</header>
                <main id="content" class="content" tabindex="-1" style="min-height:3000px">
                    <div class="content__inner"><h1 style="margin:0">Destination</h1></div>
                </main>
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
    public async Task NativeRoute_HidesOnlyContentUntilTopLanding()
    {
        await _page.EvaluateAsync("window.respondeoScroll.beginNavigation('why-god')");

        await Assertions.Expect(_page.Locator("#content")).ToHaveCSSAsync("visibility", "hidden");
        await Assertions.Expect(_page.Locator("header")).ToBeVisibleAsync();
        Assert.Equal(3000, await _page.Locator("#content").EvaluateAsync<int>("el => el.offsetHeight"));

        await _page.EvaluateAsync("window.respondeoScroll.land('top', false, false)");

        await Assertions.Expect(_page.Locator("#content")).ToHaveCSSAsync("visibility", "visible");
        Assert.Equal(0, await _page.EvaluateAsync<int>("window.scrollY"));
        await Assertions.Expect(_page.Locator("#content")).ToBeFocusedAsync();
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
