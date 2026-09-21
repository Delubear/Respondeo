using Respondeo.Content.Abstractions;

namespace Respondeo;

/// <summary>
/// Builds the relative URLs for content nodes in one place, so every link site (cards,
/// breadcrumbs, branch links) stays consistent. Nodes that belong to a stage are nested under
/// the stage slug (e.g. <c>why-god/node/aquinas-five-ways</c>) so the URL reflects the section
/// and the masthead tab lights up via its prefix match; nodes at the content root fall back to
/// the flat <c>node/{id}</c> route.
/// </summary>
public static class ContentRoutes
{
    /// <summary>Builds the href for a node, nesting under its stage when it has one.</summary>
    public static string NodeHref(ContentNode node) => NodeHref(node.Id, node.Stage);

    /// <summary>Builds the href for a node id and optional stage.</summary>
    public static string NodeHref(string id, string? stage) =>
        string.IsNullOrEmpty(stage) ? $"node/{id}" : $"{stage}/node/{id}";
}
