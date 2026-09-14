using Microsoft.Playwright;
using Reqnroll;

namespace Respondeo.AcceptanceTests.Support;

/// <summary>
/// Owns the Playwright browser/page lifecycle for a scenario and exposes the base URL under test.
/// The base URL is read from the RESPONDEO_BASE_URL environment variable so the same tests can run
/// against a locally served publish output in CI or a dev server locally (defaults to localhost:5000).
/// </summary>
public sealed class PlaywrightContext : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public IPage Page { get; private set; } = default!;

    public string BaseUrl { get; } = Environment.GetEnvironmentVariable("RESPONDEO_BASE_URL")?.TrimEnd('/') ?? "http://localhost:5000";

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();

        var headless = Environment.GetEnvironmentVariable("RESPONDEO_HEADED") != "1";
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless });
        Page = await _browser.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }
}

[Binding]
public sealed class PlaywrightHooks(PlaywrightContext context)
{
    [BeforeScenario]
    public Task BeforeScenario() => context.InitializeAsync();

    [AfterScenario]
    public async Task AfterScenario() => await context.DisposeAsync();
}
