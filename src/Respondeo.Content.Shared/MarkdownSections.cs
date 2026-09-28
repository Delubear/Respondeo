namespace Respondeo.Content.Abstractions;

/// <summary>A titled slice of a Markdown body: the heading text and the raw Markdown beneath it.</summary>
/// <param name="Heading">The section heading (empty for the lead-in content before the first heading).</param>
/// <param name="Markdown">The trimmed Markdown content of the section (never empty).</param>
public readonly record struct MarkdownSection(string Heading, string Markdown);

/// <summary>
/// Shared helper that splits a Markdown body into titled sections on each top-level "## " heading.
/// Content before the first heading (if any) becomes an untitled lead-in section with an empty
/// heading. Returns raw Markdown per section so each pillar renders it with its own renderer into
/// its own section model. Performs no rendering or I/O.
/// </summary>
public static class MarkdownSections
{
    /// <summary>Splits <paramref name="body"/> into sections on each top-level "## " heading.</summary>
    public static IReadOnlyList<MarkdownSection> Split(string body)
    {
        var lines = body.Replace("\r\n", "\n").Split('\n');
        var sections = new List<MarkdownSection>();

        var currentHeading = string.Empty;
        var buffer = new List<string>();

        void Flush()
        {
            if (buffer.Count == 0)
            {
                return;
            }

            var markdown = string.Join('\n', buffer).Trim();
            buffer.Clear();
            if (markdown.Length == 0)
            {
                return;
            }

            sections.Add(new MarkdownSection(currentHeading, markdown));
        }

        foreach (var line in lines)
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                Flush();
                currentHeading = line[3..].Trim();
                continue;
            }

            buffer.Add(line);
        }

        Flush();
        return sections;
    }
}
