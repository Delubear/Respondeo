namespace Respondeo.Services;

/// <summary>
/// A self-describing sort choice: pairs a dropdown value/label with the ordering it applies.
/// Lives alongside the browse services (not the UI) because it is a pure sorting descriptor with no Blazor dependency,
/// so browse services can expose and consume it without importing the UI namespace.
/// Rendered by the <c>SortSelect&lt;T&gt;</c> component.
/// </summary>
public sealed record SortOption<T>(string Key, string Label, Func<IEnumerable<T>, IOrderedEnumerable<T>> Apply);
