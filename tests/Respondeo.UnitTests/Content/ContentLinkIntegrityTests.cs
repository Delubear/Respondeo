using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Respondeo.Content.Abstractions;
using Respondeo.Content.Markdown.Services;

namespace Respondeo.UnitTests.Content;

/// <summary>
/// Guards against broken internal links: every reference from one content node to another must point at a node that actually exists.
/// This covers all three ways a node can link internally:
///   1. front-matter <c>branches[].to</c> (next-step cards),
///   2. front-matter <c>sections[]</c> (child accordion panels),
///   3. in-body Markdown links of the form <c>[label](node/&lt;id&gt;)</c>.
/// Catching these at test time turns a would-be runtime "empty state / dead link" into a build failure.
/// </summary>
public partial class ContentLinkIntegrityTests
{
    [GeneratedRegex("href=\"node/(?<id>[^\"#?]+)")]
    private static partial Regex NodeHrefPattern();

    private static string ContentDirectory([CallerFilePath] string thisFile = "")
    {
        // This file lives at <repo>/tests/Respondeo.UnitTests/Content/ContentLinkIntegrityTests.cs.
        // Walk up to the repo root, then into the content library's src/.../wwwroot/content folder.
        var repoRoot = Directory.GetParent(thisFile)!.Parent!.Parent!.Parent!.FullName;
        return Path.Combine(repoRoot, "src", "Respondeo.Content.Markdown", "wwwroot", "content");
    }

    private static IReadOnlyList<string> ReadManifestFiles(string contentDir)
    {
        var manifestPath = Path.Combine(contentDir, "manifest.json");
        var json = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<ContentManifest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return manifest?.Files ?? [];
    }

    /// <summary>
    /// Returns a labeled description of every internal link that does not resolve to a node in the set.
    /// Checks branches, sections, and in-body node hrefs. An empty result means all links are valid.
    /// </summary>
    private static IReadOnlyList<string> FindBrokenLinks(IReadOnlyCollection<ContentNode> nodes)
    {
        var knownIds = nodes.Select(n => n.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var brokenLinks = new List<string>();

        foreach (var node in nodes)
        {
            foreach (var branch in node.Branches.Where(b => !knownIds.Contains(b.To)))
            {
                brokenLinks.Add($"{node.Id}: branch -> '{branch.To}'");
            }

            foreach (var section in node.Sections.Where(s => !knownIds.Contains(s)))
            {
                brokenLinks.Add($"{node.Id}: section -> '{section}'");
            }

            foreach (Match match in NodeHrefPattern().Matches(node.BodyHtml))
            {
                var target = match.Groups["id"].Value;
                if (!knownIds.Contains(target))
                {
                    brokenLinks.Add($"{node.Id}: body link -> 'node/{target}'");
                }
            }
        }

        return brokenLinks;
    }

    [Fact]
    public void Every_internal_link_points_at_an_existing_node()
    {
        var contentDir = ContentDirectory();
        var parser = new ContentParser();

        // Parse only the shipped (manifest-listed) files: those are the nodes the app can actually serve.
        var nodes = ReadManifestFiles(contentDir)
            .Select(file => parser.Parse(File.ReadAllText(Path.Combine(contentDir, file))))
            .Where(node => node is not null)
            .Select(node => node!)
            .ToList();

        var brokenLinks = FindBrokenLinks(nodes);

        Assert.True(brokenLinks.Count == 0, $"Broken internal links found:{Environment.NewLine}{string.Join(Environment.NewLine, brokenLinks)}");
    }

    [Fact]
    public void Detects_a_broken_branch_link()
    {
        var nodes = new[]
        {
            Parse("---\nid: home\ntitle: Home\nbranches:\n  - to: does-not-exist\n---\nBody"),
        };

        var broken = FindBrokenLinks(nodes);

        Assert.Contains("home: branch -> 'does-not-exist'", broken);
    }

    [Fact]
    public void Detects_a_broken_section_link()
    {
        var nodes = new[]
        {
            Parse("---\nid: home\ntitle: Home\nsections:\n  - missing-section\n---\nBody"),
        };

        var broken = FindBrokenLinks(nodes);

        Assert.Contains("home: section -> 'missing-section'", broken);
    }

    [Fact]
    public void Detects_a_broken_in_body_markdown_link()
    {
        var nodes = new[]
        {
            Parse("---\nid: home\ntitle: Home\n---\nSee [this](node/ghost) page."),
        };

        var broken = FindBrokenLinks(nodes);

        Assert.Contains("home: body link -> 'node/ghost'", broken);
    }

    [Fact]
    public void Accepts_links_that_resolve_to_existing_nodes()
    {
        var nodes = new[]
        {
            Parse("---\nid: home\ntitle: Home\nbranches:\n  - to: target\nsections:\n  - target\n---\nSee [target](node/target)."),
            Parse("---\nid: target\ntitle: Target\n---\nBody"),
        };

        var broken = FindBrokenLinks(nodes);

        Assert.Empty(broken);
    }

    private static ContentNode Parse(string raw) => new ContentParser().Parse(raw)!;

    private sealed class ContentManifest
    {
        public List<string> Files { get; set; } = new();
    }
}
