using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Respondeo.Content.Shared;

/// <summary>
/// How a content pillar wants the browser/CDN to treat its static-asset requests.
/// </summary>
public enum ContentCachePolicy
{
    /// <summary>
    /// Send a <c>no-cache</c> request directive so the browser revalidates with the origin.
    /// Used by pillars whose static files can be updated in place on hosts where server cache headers cannot be set (e.g. GitHub Pages),
    /// so users never run against an outdated manifest or node.
    /// </summary>
    Revalidate,

    /// <summary>
    /// Opt into the browser cache for a year.
    /// Used by pillars whose bundled catalog is immutable for the lifetime of a deploy and whose static assets are fingerprinted per build,
    /// so repeat visits reuse the stored copy with no risk of serving stale content across deploys.
    /// </summary>
    Immutable,
}

/// <summary>
/// Shared HTTP helper that fetches a content pillar's static assets with a consistent cache policy.
/// Centralizes the fetch-with-cache-directive plumbing that every pillar's loader would otherwise duplicate,
/// while leaving each pillar free to choose its <see cref="ContentCachePolicy"/>.
/// </summary>
public sealed class ContentFetcher(HttpClient http, ContentCachePolicy policy)
{
    /// <summary>Fetches the resource at <paramref name="url"/> as a string, honoring the cache policy.</summary>
    public async Task<string> GetStringAsync(string url)
    {
        using var response = await SendAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>Fetches and deserializes the JSON resource at <paramref name="url"/>, honoring the cache policy.</summary>
    public async Task<T?> GetFromJsonAsync<T>(string url)
    {
        using var response = await SendAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private Task<HttpResponseMessage> SendAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Headers = { CacheControl = CacheControlFor(policy) }
        };
        return http.SendAsync(request);
    }

    private static CacheControlHeaderValue CacheControlFor(ContentCachePolicy policy) => policy switch
    {
        ContentCachePolicy.Immutable => new CacheControlHeaderValue { MaxAge = TimeSpan.FromDays(365) },
        _ => new CacheControlHeaderValue { NoCache = true },
    };
}
