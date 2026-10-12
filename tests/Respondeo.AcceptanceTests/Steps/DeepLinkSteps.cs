using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

// Checks the raw HTTP response (status and the static canonical tag) that crawlers see before any
// JavaScript runs. These guard the deploy's per-route entry pages, so they only pass against the
// published site served with GitHub Pages-style lookup (see .github/actions/acceptance-tests/serve.py).
[Binding]
public sealed class DeepLinkSteps(PlaywrightContext context)
{
    private IAPIResponse? _response;
    private string _body = string.Empty;

    [When("I request {string} directly")]
    public async Task WhenIRequestDirectly(string path)
    {
        _response = await context.Page.APIRequest.GetAsync($"{context.BaseUrl}/{path}");
        _body = await _response.TextAsync();
    }

    [Then("the response status should be {int}")]
    public void ThenTheResponseStatusShouldBe(int status)
    {
        Assert.Equal(status, _response!.Status);
    }

    [Then("the canonical URL should end with {string}")]
    public void ThenTheCanonicalUrlShouldEndWith(string path)
    {
        Assert.Contains($"<link rel=\"canonical\" href=\"https://respondeo.faith/{path}\" />", _body);
    }
}
