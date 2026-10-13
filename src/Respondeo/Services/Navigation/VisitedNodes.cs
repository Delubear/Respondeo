using System.Text.Json;
using Microsoft.JSInterop;

namespace Respondeo.Services.Navigation;

/// <summary>
/// Tracks the set of content nodes a visitor has opened during the current journey, so the UI can mark "where to go next" cards that lead to a node already read.
/// Unlike <see cref="IBreadcrumbTrail"/>, which reflects the current route and truncates on back-navigation,
/// this accumulates every node seen and only resets when the journey does (returning to the Start page).
/// </summary>
public interface IVisitedNodes
{
    /// <summary>
    /// Records that <paramref name="nodeId"/> has been visited. Idempotent: visiting an already-recorded node is a no-op.
    /// </summary>
    Task MarkVisitedAsync(string nodeId);

    /// <summary>
    /// Loads the set of visited node ids for the current journey.
    /// </summary>
    Task<IReadOnlySet<string>> GetVisitedAsync();

    /// <summary>
    /// Clears all visited nodes. Called when the visitor returns to the Start page so a new journey begins fresh.
    /// </summary>
    Task ClearAsync();
}

/// <summary>
/// An <see cref="IVisitedNodes"/> that persists the visited set in the browser's <c>sessionStorage</c>.
/// This mirrors <see cref="BreadcrumbTrail"/>'s storage choice: it survives accidental refreshes within the same tab and keeps URLs clean,
/// while a shared or deep-linked URL naturally starts a fresh journey.
/// </summary>
public sealed class VisitedNodes(IJSRuntime js) : IVisitedNodes
{
    private const string StorageKey = "respondeo.visited";

    public async Task MarkVisitedAsync(string nodeId)
    {
        var visited = await LoadAsync();
        if (visited.Add(nodeId))
        {
            await SaveAsync(visited);
        }
    }

    public async Task<IReadOnlySet<string>> GetVisitedAsync() => await LoadAsync();

    public async Task ClearAsync() => await js.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);

    private async Task<HashSet<string>> LoadAsync()
    {
        try
        {
            var json = await js.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);
            return string.IsNullOrWhiteSpace(json)
                ? new HashSet<string>(StringComparer.Ordinal)
                : JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>(StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }
    }

    private async Task SaveAsync(HashSet<string> visited)
    {
        var json = JsonSerializer.Serialize(visited);
        await js.InvokeVoidAsync("sessionStorage.setItem", StorageKey, json);
    }
}
