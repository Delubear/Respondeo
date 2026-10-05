using Respondeo.Components;
using Respondeo.Content.Contracts;

namespace Respondeo.Services;

/// <summary>
/// Pure, stateless browse logic for the devotions index: kind formatting, facet derivation, and the combined search/filter/sort.
/// Kept out of <see cref="Pages.Devotions"/> so it can be unit-tested, mirroring <see cref="PrayerBrowse"/>.
/// </summary>
public sealed class DevotionBrowse
{
    /// <summary>The neutral bucket that carries no pill and is never offered as a filterable facet.</summary>
    private const string OtherKind = "devotion";

    /// <summary>
    /// Turns a kind slug (e.g. "rosary") into a display label (e.g. "Rosary"), or null for the blank / "devotion" bucket so those cards simply carry no pill.
    /// </summary>
    public string? FormatKind(string kind) => string.IsNullOrWhiteSpace(kind) || kind == OtherKind ? null : char.ToUpperInvariant(kind[0]) + kind[1..];

    /// <summary>
    /// The distinct, alphabetically ordered kind slugs that actually occur in the index, excluding the neutral "devotion" bucket so only meaningful facets are offered.
    /// </summary>
    public IReadOnlyList<string> Kinds(IEnumerable<DevotionSummary> devotions) =>
        [.. devotions.Select(d => d.Kind)
            .Where(k => !string.IsNullOrWhiteSpace(k) && k != OtherKind)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => k, StringComparer.CurrentCultureIgnoreCase)];

    /// <summary>The display labels for <see cref="Kinds"/>, in the same order.</summary>
    public IReadOnlyList<string> KindLabels(IEnumerable<string> kinds) => [.. kinds.Select(k => FormatKind(k) ?? k)];

    /// <summary>
    /// Applies the active kind facet, free-text query, and sort to the index.
    /// Kinds are selected by their display label (matched back to the slug); the query matches title, summary, and kind label.
    /// </summary>
    public IReadOnlyList<DevotionSummary> Filter(IReadOnlyList<DevotionSummary> devotions, string query, IReadOnlySet<string> selectedKinds, SortOption<DevotionSummary> sort)
    {
        IEnumerable<DevotionSummary> results = devotions;

        if (selectedKinds.Count > 0)
        {
            results = results.Where(d => selectedKinds.Contains(FormatKind(d.Kind) ?? d.Kind));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            results = results.Where(d =>
                d.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                || d.Summary.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (FormatKind(d.Kind)?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return [.. sort.Apply(results)];
    }
}
