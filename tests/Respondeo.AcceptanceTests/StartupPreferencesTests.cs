using System.Text.Json;
using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests;

public sealed class StartupPreferencesTests
{
    [Theory]
    [InlineData(null, null, false, "light", "default")]
    [InlineData("dark", "large", false, "dark", "large")]
    [InlineData("light", "small", false, "light", "small")]
    [InlineData(null, null, true, "light", "default")]
    public async Task Published_shell_initializes_preferences_before_deferred_scripts(
        string? theme, string? textSize, bool storageBlocked, string expectedTheme, string expectedTextSize)
    {
        await using var context = new PlaywrightContext();
        await context.InitializeAsync(nameof(Published_shell_initializes_preferences_before_deferred_scripts));
        var page = context.Page;
        var response = await page.APIRequest.GetAsync(context.BaseUrl + "/");
        Assert.True(response.Ok, $"Unable to read the published shell: HTTP {response.Status}");
        var html = await response.TextAsync();
        await response.DisposeAsync();
        await page.AddInitScriptAsync($$"""
            (() => {
                const settings = {{JsonSerializer.Serialize(new { theme, textSize, storageBlocked })}};
                if (settings.storageBlocked) {
                    Object.defineProperty(window, 'localStorage', { get() { throw new DOMException('Storage blocked', 'SecurityError'); } });
                } else {
                    localStorage.clear();
                    if (settings.theme) localStorage.setItem('respondeo.theme', settings.theme);
                    if (settings.textSize) localStorage.setItem('respondeo.textsize', settings.textSize);
                }
            })();
            """);
        await page.RouteAsync("**/*", route =>
        {
            if (route.Request.ResourceType == "document")
            {
                return route.FulfillAsync(new RouteFulfillOptions { ContentType = "text/html", Body = html });
            }
            if (route.Request.ResourceType == "script")
            {
                return route.FulfillAsync(new RouteFulfillOptions
                {
                    ContentType = "application/javascript",
                    Body = """
                        if (!window.firstDeferredPreferences) {
                            window.firstDeferredPreferences = {
                                theme: document.documentElement.getAttribute('data-theme'),
                                size: document.documentElement.getAttribute('data-textsize'),
                                js: document.documentElement.classList.contains('js')
                            };
                        }
                        """
                });
            }
            return route.FulfillAsync(new RouteFulfillOptions { Body = "" });
        });

        await page.GotoAsync(context.BaseUrl + "/");

        Assert.Equal(expectedTheme, await page.EvaluateAsync<string>("window.firstDeferredPreferences.theme"));
        Assert.Equal(expectedTextSize, await page.EvaluateAsync<string>("window.firstDeferredPreferences.size"));
        Assert.True(await page.EvaluateAsync<bool>("window.firstDeferredPreferences.js"));
        await Assertions.Expect(page.Locator("#blazor-error-ui")).ToHaveAttributeAsync("role", "alert");
        await Assertions.Expect(page.Locator("#blazor-error-ui .reload")).ToHaveTextAsync("Reload");
        await Assertions.Expect(page.Locator("#blazor-error-ui .dismiss")).ToHaveAttributeAsync("aria-label", "Dismiss");
    }
}
