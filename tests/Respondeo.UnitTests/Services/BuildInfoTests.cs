using System.Reflection;
using System.Text.Json;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class BuildInfoTests
{
    [Fact]
    public void Reads_the_exact_embedded_host_identity()
    {
        var assembly = Assembly.Load("Respondeo");
        using var stream = assembly.GetManifestResourceStream("Respondeo.BuildManifest.json")!;
        using var document = JsonDocument.Parse(stream);
        var manifest = document.RootElement;

        var info = BuildInfo.Load(assembly);

        Assert.Equal(manifest.GetProperty("version").GetString(), info.Version);
        Assert.Equal(manifest.GetProperty("sourceRevision").GetString(), info.SourceRevision);
        Assert.Equal("wasm", info.Host);
        Assert.Equal(manifest.GetProperty("buildKind").GetString(), info.BuildKind);
        Assert.Equal(manifest.GetProperty("contentSha256").GetString(), info.ContentSha256);
        Assert.Matches("^[0-9a-f]{64}$", info.ContentSha256);
        Assert.Equal(manifest.GetProperty("hoursMode").GetString(), info.HoursMode);
        Assert.Equal(manifest.GetProperty("hoursRevision").GetString(), info.HoursRevision);
    }

    [Fact]
    public void Missing_manifest_fails_explicitly()
    {
        var error = Assert.Throws<InvalidOperationException>(() => BuildInfo.Load(typeof(BuildInfo).Assembly));

        Assert.Equal("The host build manifest is missing.", error.Message);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{}")]
    [InlineData("{\"version\":\"1.0.42\"}")]
    [InlineData("{\"version\":null,\"sourceRevision\":\"development\",\"host\":\"wasm\",\"buildKind\":\"development\"," +
        "\"contentSha256\":\"fixture\",\"hoursMode\":\"sample\",\"hoursRevision\":\"sample\"}")]
    public void Malformed_or_incomplete_identity_is_not_accepted(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, BuildInfoJsonContext.Default.BuildInfo));
    }
}
