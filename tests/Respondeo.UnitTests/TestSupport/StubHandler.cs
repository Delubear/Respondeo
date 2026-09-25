using System.Net;
using System.Text;

namespace Respondeo.UnitTests.TestSupport;

/// <summary>
/// Shared <see cref="HttpMessageHandler"/> test double that serves canned responses keyed by request
/// path. Content-type is derived from the file extension (".json" -> application/json, otherwise
/// text/plain) so callers using <c>GetFromJsonAsync</c> and raw-text loaders both work unchanged.
/// Unknown paths return 404.
/// </summary>
internal sealed class StubHandler(IReadOnlyDictionary<string, string> responses) : HttpMessageHandler
{
    private readonly Dictionary<string, int> _counts = new();

    /// <summary>Number of requests seen for a given path; used to assert caching behavior.</summary>
    public int RequestCount(string path) => _counts.TryGetValue(path, out var count) ? count : 0;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath.TrimStart('/');
        _counts[path] = RequestCount(path) + 1;

        if (responses.TryGetValue(path, out var body))
        {
            var contentType = path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? "application/json" : "text/plain";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, contentType),
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
