using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Respondeo.Components;
using Respondeo.Services;

namespace Respondeo.UnitTests.Components;

public class BuildDetailsTests : TestContext
{
    [Theory]
    [InlineData("wasm", "sample", "sample")]
    [InlineData("android", "licensed", "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")]
    public void Displays_full_offline_identity_in_a_collapsed_disclosure(string host, string mode, string revision)
    {
        var info = new BuildInfo("1.0.42", new string('a', 40), host, "ci", new string('c', 64), mode, revision);
        Services.AddSingleton(info);

        var cut = RenderComponent<BuildDetails>();

        Assert.False(cut.Find("details").HasAttribute("open"));
        Assert.Equal("Version & build details", cut.Find("summary").TextContent);
        Assert.Equal(new[] { "Version", "Host", "Source commit", "Content SHA-256", "Hours content", "Hours revision" },
            cut.FindAll("dt").Select(element => element.TextContent));
        Assert.Equal(new[] { "1.0.42 (ci)", host, info.SourceRevision, info.ContentSha256, mode, revision },
            cut.FindAll("dd").Select(element => element.TextContent));
    }
}
