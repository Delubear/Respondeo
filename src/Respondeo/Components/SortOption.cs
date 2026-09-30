namespace Respondeo.Components;

/// <summary>
/// A single, self-describing sort choice for a browse page.
/// Each option pairs the dropdown's value/label with the ordering it performs,
/// so a page's sort modes are declared in one place and rendered consistently by <see cref="SortSelect{T}"/>.
/// </summary>
/// <typeparam name="T">The row type being ordered.</typeparam>
/// <param name="Key">Stable value used as the option's <c>&lt;option value&gt;</c> and the page's current-sort key.</param>
/// <param name="Label">Human-readable text shown in the dropdown.</param>
/// <param name="Apply">Applies this option's ordering to a sequence of rows.</param>
public sealed record SortOption<T>(string Key, string Label, Func<IEnumerable<T>, IOrderedEnumerable<T>> Apply);
