namespace Respondeo.Navigation;

/// <summary>
/// Shared route parsing and route-name constants, so components that branch on the current route
/// (e.g. <see cref="FloatingGuideRoutes"/> and the route prefetcher) agree on how a URL is read and on the route names they match.
/// </summary>
public static class RoutePath
{
    public const string Summa = "summa";
    public const string Bible = "bible";
    public const string Discover = "discover";
    public const string Articles = "articles";
    public const string Saints = "saints";
    public const string Prayers = "prayers";
    public const string Miracles = "miracles";
    public const string Devotions = "devotions";

    /// <summary>Drops any query/fragment and trims surrounding slashes. Case is preserved.</summary>
    public static string StripQueryAndFragment(string relativePath)
    {
        var cut = relativePath.IndexOfAny(['?', '#']);
        var path = cut >= 0 ? relativePath[..cut] : relativePath;
        return path.Trim('/');
    }

    /// <summary>Splits a relative URL into its path segments (query/fragment removed, case preserved).</summary>
    public static string[] Segments(string relativePath) => StripQueryAndFragment(relativePath).Split('/', StringSplitOptions.RemoveEmptyEntries);
}
