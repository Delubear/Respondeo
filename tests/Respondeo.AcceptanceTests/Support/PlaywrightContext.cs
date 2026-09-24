using Microsoft.Playwright;

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

    public async Task InitializeAsync(string scenarioName)
    {
        _playwright = await Playwright.CreateAsync();

        var headless = Environment.GetEnvironmentVariable("RESPONDEO_HEADED") != "1";
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless });
        // Local dev servers use a self-signed HTTPS certificate; ignore cert errors so tests
        // can run against https://localhost without a trusted dev cert in the test environment.
        Page = await _browser.NewPageAsync(new BrowserNewPageOptions { IgnoreHTTPSErrors = true });
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
    public Task BeforeScenario(ScenarioContext scenario) => context.InitializeAsync(scenario.ScenarioInfo.Title);

    [AfterScenario]
    public async Task AfterScenario() => await context.DisposeAsync();
}
