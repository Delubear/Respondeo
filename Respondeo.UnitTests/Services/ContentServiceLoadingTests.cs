using System.Net;
using System.Text;
using Respondeo.Content.Markdown.Services;

namespace Respondeo.UnitTests.Services;

public class ContentServiceLoadingTests
{
    private const string Manifest = "{\"files\":[\"home.md\",\"branch.md\"]}";

    private const string HomeMd = "---\nid: home\ntitle: Home\nisEntryPoint: true\nbranches:\n  - to: branch\n---\nWelcome home";

    private const string BranchMd = "---\nid: branch\ntitle: Branch\nisEntryPoint: false\n---\nA branch node";

    private static ContentService CreateService()
    {
        var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Content.Markdown/content/manifest.json"] = Manifest,
            ["_content/Respondeo.Content.Markdown/content/home.md"] = HomeMd,
            ["_content/Respondeo.Content.Markdown/content/branch.md"] = BranchMd,
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new ContentService(http, new ContentParser());
    }

    [Fact]
    public async Task GetById_returns_parsed_node_with_rendered_html()
    {
        var service = CreateService();

        var node = await service.GetByIdAsync("home");

        Assert.NotNull(node);
        Assert.Equal("Home", node!.Title);
        Assert.Contains("Welcome home", node.BodyHtml);
    }

    [Fact]
    public async Task GetById_is_case_insensitive()
    {
        var service = CreateService();

        var node = await service.GetByIdAsync("HOME");

        Assert.NotNull(node);
    }

    [Fact]
    public async Task GetById_returns_null_for_unknown_id()
    {
        var service = CreateService();

        var node = await service.GetByIdAsync("missing");

        Assert.Null(node);
    }

    [Fact]
    public async Task GetEntryPoints_returns_only_entry_point_nodes()
    {
        var service = CreateService();

        var entries = await service.GetEntryPointsAsync();

        Assert.Single(entries);
        Assert.Equal("home", entries[0].Id);
    }

    [Fact]
    public async Task GetAll_returns_every_node()
    {
        var service = CreateService();

        var all = await service.GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    private sealed class StubHandler(IReadOnlyDictionary<string, string> responses) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath.TrimStart('/');
            if (responses.TryGetValue(path, out var body))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "text/plain"),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
