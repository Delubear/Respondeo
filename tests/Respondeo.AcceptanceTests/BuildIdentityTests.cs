using System.Text.Json;
using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests;

public sealed class BuildIdentityTests
{
    [Fact]
    [Trait("Category", "Published")]
    public async Task Cached_pwa_reports_its_own_identity_after_an_offline_reload()
    {
        await using var context = new PlaywrightContext();
        await context.InitializeAsync(nameof(Cached_pwa_reports_its_own_identity_after_an_offline_reload));
        var page = context.Page;
        var site = new UriBuilder(context.BaseUrl);
        // Bare localhost intentionally unregisters workers during development; Chromium resolves *.localhost to loopback.
        if (site.Uri.IsLoopback)
        {
            site.Host = "respondeo.localhost";
        }
        await page.RouteAsync("https://static.cloudflareinsights.com/**", route => route.AbortAsync());
        await page.GotoAsync(site.Uri.AbsoluteUri);
        await Assertions.Expect(page.GetByRole(AriaRole.Contentinfo)).ToBeVisibleAsync();
        var disclosure = page.Locator(".build-details");
        await Assertions.Expect(disclosure).ToHaveCountAsync(0);
        await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Thanks & sources", Exact = true }).ClickAsync();
        await Assertions.Expect(disclosure.Locator("summary")).ToHaveTextAsync("Version & build details");
        await Assertions.Expect(disclosure).Not.ToHaveAttributeAsync("open", "");
        await Assertions.Expect(page.GetByRole(AriaRole.Contentinfo).Locator(".build-details")).ToHaveCountAsync(0);
        using var manifest = JsonDocument.Parse(await page.EvaluateAsync<string>("async () => (await fetch('build-manifest.json')).text()"));
        var identity = manifest.RootElement;
        var expected = new[]
        {
            $"{identity.GetProperty("version").GetString()} ({identity.GetProperty("buildKind").GetString()})",
            "wasm",
            identity.GetProperty("sourceRevision").GetString()!,
            identity.GetProperty("contentSha256").GetString()!,
            identity.GetProperty("hoursMode").GetString()!,
            identity.GetProperty("hoursRevision").GetString()!
        };
        await page.WaitForFunctionAsync("() => navigator.serviceWorker.controller !== null", null,
            new PageWaitForFunctionOptions { Timeout = 120_000 });
        await page.Context.SetOfflineAsync(true);

        await page.ReloadAsync();
        await disclosure.Locator("summary").ClickAsync();

        await Assertions.Expect(disclosure).ToHaveAttributeAsync("open", "");
        await Assertions.Expect(disclosure.Locator("dd")).ToHaveTextAsync(expected);
        Assert.Matches("^[0-9a-f]{64}$", expected[3]);

        await page.GotoAsync(site.Uri.AbsoluteUri);
        await Assertions.Expect(page.GetByRole(AriaRole.Contentinfo)).ToBeVisibleAsync();
        await Assertions.Expect(disclosure).ToHaveCountAsync(0);
        await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Thanks & sources", Exact = true }).ClickAsync();
        await disclosure.Locator("summary").ClickAsync();
        await Assertions.Expect(disclosure.Locator("dd")).ToHaveTextAsync(expected);
    }
}
