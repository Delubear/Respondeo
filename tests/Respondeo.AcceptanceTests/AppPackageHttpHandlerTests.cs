using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using Respondeo.Maui;
using Xunit;

namespace Respondeo.AcceptanceTests;

public sealed class AppPackageHttpHandlerTests
{
    [Fact]
    public async Task Package_stream_drives_the_response_without_a_network_handler()
    {
        string? opened = null;
        using var source = new MemoryStream(Encoding.UTF8.GetBytes("packaged content"));
        using var client = new HttpClient(new AppPackageHttpHandler(path =>
        {
            opened = path;
            return Task.FromResult<Stream>(source);
        })) { BaseAddress = new Uri("https://0.0.0.0/") };

        using var response = await client.GetAsync("_content/sample%20file.md?ignored=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("packaged content", await response.Content.ReadAsStringAsync());
        Assert.Equal("wwwroot/_content/sample file.md", opened);
        Assert.False(source.CanRead);
    }

    [Fact]
    public async Task Missing_package_file_returns_not_found()
    {
        using var client = new HttpClient(new AppPackageHttpHandler(_ => throw new FileNotFoundException()))
        { BaseAddress = new Uri("https://0.0.0.0/") };

        using var response = await client.GetAsync("missing.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("https://0.0.0.0/missing.json", response.RequestMessage!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task Unexpected_package_errors_are_not_reported_as_missing_content()
    {
        var failure = new InvalidOperationException("package failure");
        using var client = new HttpClient(new AppPackageHttpHandler(_ => throw failure))
        { BaseAddress = new Uri("https://0.0.0.0/") };

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("content.json")));
    }

    [Fact]
    public async Task Canceled_package_open_does_not_return_success()
    {
        using var client = new HttpClient(new AppPackageHttpHandler(_ =>
        {
            throw new OperationCanceledException();
        })) { BaseAddress = new Uri("https://0.0.0.0/") };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync("content.json"));
    }
}

public sealed class AndroidPackageTransportTests
{
    [Theory]
    [Trait("Category", "AndroidArtifact")]
    [InlineData("_content/Respondeo.Content/inquiry/manifest.json")]
    [InlineData("_content/Respondeo.Content/discover/miracles/manifest.json")]
    [InlineData("_content/Respondeo.Content/discover/prayers/manifest.json")]
    public async Task Published_manifests_and_their_articles_load_through_the_native_transport(string manifestPath)
    {
        var apk = Environment.GetEnvironmentVariable("RESPONDEO_APK_PATH")
            ?? throw new InvalidOperationException("Set RESPONDEO_APK_PATH to the final APK when running Category=AndroidArtifact tests.");
        using var archive = ZipFile.OpenRead(apk);
        var opened = new List<string>();
        using var client = new HttpClient(new AppPackageHttpHandler(path =>
        {
            opened.Add(path);
            var entry = archive.GetEntry("assets/" + path) ?? throw new FileNotFoundException(path);
            return Task.FromResult(entry.Open());
        })) { BaseAddress = new Uri("https://0.0.0.0/") };

        using var manifestResponse = await client.GetAsync(manifestPath);
        Assert.Equal(HttpStatusCode.OK, manifestResponse.StatusCode);
        using var manifest = JsonDocument.Parse(await manifestResponse.Content.ReadAsByteArrayAsync());
        var files = manifest.RootElement.GetProperty("files").EnumerateArray().Select(file => file.GetString()!).ToArray();
        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var uri = new Uri(new Uri(client.BaseAddress, manifestPath), file);
            using var response = await client.GetAsync(uri);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsByteArrayAsync();
            Assert.NotEmpty(body);
            using var expected = archive.GetEntry("assets/wwwroot/" + Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')))!.Open();
            using var bytes = new MemoryStream();
            await expected.CopyToAsync(bytes);
            Assert.Equal(bytes.ToArray(), body);
        }
        Assert.Equal(files.Length + 1, opened.Count);
        Assert.Equal("wwwroot/" + manifestPath, opened[0]);
    }
}
