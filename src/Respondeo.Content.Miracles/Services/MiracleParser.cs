using Markdig;
using Respondeo.Content.Miracles.Internal;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Respondeo.Content.Miracles.Services;

/// <summary>
/// Turns a raw miracle Markdown file (with a "---" delimited YAML front-matter block) into the typed
/// <see cref="MiracleRecord"/> and its lightweight <see cref="MiracleIndexEntry"/> projection.
/// Owns the YAML deserializer and Markdig pipeline; performs no I/O so it can be tested in isolation.
/// The body is split into titled sections on top-level "## " headings.
/// </summary>
internal sealed class MiracleParser
{
    private readonly IDeserializer _yaml = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private readonly MarkdownPipeline _markdown = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>
    /// Parses raw file content into a miracle record, or returns null when there is no valid
    /// front-matter block or the front-matter lacks an id.
    /// </summary>
    public MiracleRecord? Parse(string raw)
    {
        var (frontMatter, body) = SplitFrontMatter(raw);
        if (frontMatter is null)
        {
            return null;
        }

        var meta = _yaml.Deserialize<MiracleFrontMatter>(frontMatter);
        if (meta is null || string.IsNullOrWhiteSpace(meta.Id))
        {
            return null;
        }

        return new MiracleRecord
        {
            Id = meta.Id,
            Title = meta.Title,
            Summary = meta.Summary,
            Type = MiracleFacets.ParseType(meta.Type),
            Approval = MiracleFacets.ParseApproval(meta.Approval),
            Region = MiracleFacets.ParseRegion(meta.Region),
            Country = meta.Country,
            Year = meta.Year,
            FeastDay = meta.FeastDay,
            Tags = meta.Tags,
            Sections = SplitSections(body),
            Sources = [.. meta.Sources.Select(s => new MiracleSource { Label = s.Label, Url = s.Url })],
        };
    }

    /// <summary>Projects a full record down to its lightweight index entry.</summary>
    public static MiracleIndexEntry ToIndexEntry(MiracleRecord record) => new()
    {
        Id = record.Id,
        Title = record.Title,
        Summary = record.Summary,
        Type = record.Type,
        Approval = record.Approval,
        Region = record.Region,
        Country = record.Country,
        Year = record.Year,
        Tags = record.Tags,
    };

    // Split the Markdown body into sections on each top-level "## " heading. Content before the first
    // heading (if any) is rendered as an untitled lead-in section with an empty heading.
    private IReadOnlyList<MiracleSection> SplitSections(string body)
    {
        var lines = body.Replace("\r\n", "\n").Split('\n');
        var sections = new List<MiracleSection>();

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

            sections.Add(new MiracleSection
            {
                Heading = currentHeading,
                Html = Markdig.Markdown.ToHtml(markdown, _markdown),
            });
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

    /// <summary>
    /// Splits a "---" delimited YAML front-matter block from the Markdown body.
    /// Returns (null, raw) when no front-matter block is present.
    /// </summary>
    internal static (string? FrontMatter, string Body) SplitFrontMatter(string raw)
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
