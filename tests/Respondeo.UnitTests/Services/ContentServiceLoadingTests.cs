using System.Net;
using System.Text;
using Respondeo.Content.Markdown.Services;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Services;

public class ContentServiceLoadingTests
{
    private const string Manifest = "{\"files\":[\"home.md\",\"branch.md\"]}";

    private const string HomeMd = "---\nid: home\ntitle: Home\nbranches:\n  - to: branch\n---\nWelcome home";

    private const string BranchMd = "---\nid: branch\ntitle: Branch\n---\nA branch node";

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
    public async Task GetAll_returns_every_node()
    {
        var service = CreateService();

        var all = await service.GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task A_file_that_fails_to_load_is_skipped_without_breaking_the_rest()
    {
        // Manifest references a file the server does not serve (simulates a 404 from a stale
        // static-web-asset manifest). The remaining nodes must still load.
        const string manifest = "{\"files\":[\"home.md\",\"missing.md\",\"branch.md\"]}";

        var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Content.Markdown/content/manifest.json"] = manifest,
            ["_content/Respondeo.Content.Markdown/content/home.md"] = HomeMd,
            ["_content/Respondeo.Content.Markdown/content/branch.md"] = BranchMd,
            // "missing.md" is intentionally absent, so the handler returns 404 for it.
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new ContentService(http, new ContentParser());

        var all = await service.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.NotNull(await service.GetByIdAsync("home"));
        Assert.NotNull(await service.GetByIdAsync("branch"));
    }
}
