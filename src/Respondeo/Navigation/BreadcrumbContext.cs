using Respondeo.Components;

namespace Respondeo.Navigation;

/// <summary>
/// A content item that can describe its own place in a breadcrumb trail, independent of whether it
/// is a Markdown node (a graph reached by a visit trail) or a Summa question (a fixed part → question
/// hierarchy). Both content systems keep their own models; they merely project onto this small shared
/// shape so the presentational <see cref="Breadcrumb"/> component is fed the same way from either side.
/// </summary>
public interface INavigable
{
    /// <summary>The title shown as the final, non-linked crumb for this item.</summary>
    string BreadcrumbTitle { get; }
}

/// <summary>
/// The resolved breadcrumb trail for a content item: an optional section root (the area landing page
/// that replaces "Home"), the ordered ancestor crumbs leading up to the item, and the item's own title.
/// This is the single hand-off shape both the Markdown and Summa pages build before rendering
/// <see cref="Breadcrumb"/>, so the two systems stay aligned without sharing a content model.
/// </summary>
/// <param name="CurrentTitle">Title of the item currently being viewed.</param>
/// <param name="Ancestors">Ordered ancestor crumbs leading up to (but excluding) the current item.</param>
/// <param name="RootHref">Optional section landing route that replaces Home as the trail root.</param>
/// <param name="RootLabel">Label for the section root crumb.</param>
public sealed record BreadcrumbContext(
    string? CurrentTitle,
    IReadOnlyList<Breadcrumb.Crumb> Ancestors,
    string? RootHref = null,
    string? RootLabel = null)
{
    /// <summary>An empty trail with just the supplied current title and no ancestors or section root.</summary>
    public static BreadcrumbContext ForCurrent(string? currentTitle) => new(currentTitle, []);
}
