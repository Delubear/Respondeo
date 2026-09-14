using Microsoft.Playwright;
using Reqnroll;

namespace Respondeo.AcceptanceTests.Support;

/// <summary>
/// Owns the Playwright browser/page lifecycle for a scenario and exposes the base URL under test.
/// The base URL is read from the RESPONDEO_BASE_URL environment variable so the same tests can run
/// against a locally served publish output in CI or a dev server locally (defaults to localhost:5000).
/// Captures browser console output, page errors, and failed requests to make CI failures diagnosable.
/// </summary>
public sealed class PlaywrightContext : IAsyncDisposable
{
    private readonly List<string> _log = new();
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string _scenarioName = "scenario";

    public IPage Page { get; private set; } = default!;

    public string BaseUrl { get; } = Environment.GetEnvironmentVariable("RESPONDEO_BASE_URL")?.TrimEnd('/') ?? "http://localhost:5000";

    public async Task InitializeAsync(string scenarioName)
    {
        _scenarioName = scenarioName;
        _playwright = await Playwright.CreateAsync();

        var headless = Environment.GetEnvironmentVariable("RESPONDEO_HEADED") != "1";
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless });
        Page = await _browser.NewPageAsync();

        // Surface everything the browser knows so a CI failure explains itself.
        Page.Console += (_, msg) => _log.Add($"[console:{msg.Type}] {msg.Text}");
        Page.PageError += (_, error) => _log.Add($"[pageerror] {error}");
        Page.RequestFailed += (_, request) => _log.Add($"[requestfailed] {request.Failure} {request.Url}");
        Page.Response += (_, response) =>
        {
            if (response.Status >= 400)
            {
                _log.Add($"[response:{response.Status}] {response.Url}");
            }
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (Page is not null)
        {
            // Dump collected diagnostics to stdout so they appear in the CI test log.
            if (_log.Count > 0)
            {
                Console.WriteLine($"--- Browser diagnostics for '{_scenarioName}' ---");
                foreach (var line in _log)
                {
                    Console.WriteLine(line);
                }
            }

            var artifactsDir = Environment.GetEnvironmentVariable("RESPONDEO_ARTIFACTS") ?? "playwright-artifacts";
            Directory.CreateDirectory(artifactsDir);
            var safeName = string.Concat(_scenarioName.Split(Path.GetInvalidFileNameChars()));

            try
            {
                await Page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = Path.Combine(artifactsDir, $"{safeName}.png"),
                    FullPage = true,
                });
                var html = await Page.ContentAsync();
                await File.WriteAllTextAsync(Path.Combine(artifactsDir, $"{safeName}.html"), html);
            }
            catch
            {
                // Best-effort diagnostics; never mask the real test failure.
            }
        }

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
