using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

// Checks the raw HTTP response (status and the static canonical tag) that crawlers see before any
// JavaScript runs. These guard the deploy's per-route entry pages, so they only pass against the
// published site served with GitHub Pages-style lookup (see .github/actions/acceptance-tests/serve.py).
[Binding]
public sealed class DeepLinkSteps(PlaywrightContext context, Reqnroll.UnitTestProvider.IUnitTestRuntimeProvider runtime)
{
    private IAPIResponse? _response;
    private string _body = string.Empty;

    [When("I request {string} directly")]
    public async Task WhenIRequestDirectly(string path)
    {
        var probe = await context.Page.APIRequest.GetAsync($"{context.BaseUrl}/respondeo-deeplink-probe-404");
        if (probe.Status == 200)
        {
            runtime.TestIgnore("Deep link checks need the published site served by serve.py (the dev server falls back to index.html).");
            return;
        }

        _response = await context.Page.APIRequest.GetAsync($"{context.BaseUrl}/{path}");
        _body = await _response.TextAsync();
    }

    [Then("the response status should be {int}")]
    public void ThenTheResponseStatusShouldBe(int status)
    {
        if (_response is null)
        {
            return;
        }

        Assert.Equal(status, _response.Status);
    }

    [Then("the canonical URL should end with {string}")]
    public void ThenTheCanonicalUrlShouldEndWith(string path)
    {
        if (_response is null)
        {
            return;
        }

        Assert.Matches($"<link rel=\"canonical\" href=\"[^\"]*{System.Text.RegularExpressions.Regex.Escape(path)}\"", _body);
    }
}
