namespace Respondeo.Content.Abstractions;

/// <summary>
/// Shared helper for splitting a "---" delimited YAML front-matter block from a Markdown body.
/// Used by every content pillar's parser so the fenced-front-matter convention stays identical
/// across the Markdown, Summa, and Miracles content types.
/// </summary>
public static class FrontMatter
{
    /// <summary>
    /// Splits a "---" delimited YAML front-matter block from the Markdown body. Newlines are
    /// normalized and a leading BOM/whitespace is tolerated. Returns (null, raw) when no valid
    /// front-matter block is present.
    /// </summary>
    public static (string? FrontMatter, string Body) Split(string raw)
    {
        var text = raw.Replace("\r\n", "\n").TrimStart('\uFEFF', ' ', '\n');
        if (!text.StartsWith("---\n", StringComparison.Ordinal))
        {
            return (null, raw);
        }

        var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0)
        {
            return (null, raw);
        }

        var frontMatter = text.Substring(4, end - 4);
        var bodyStart = text.IndexOf('\n', end + 1);
        var body = bodyStart < 0 ? string.Empty : text[(bodyStart + 1)..];
        return (frontMatter, body);
    }
}
