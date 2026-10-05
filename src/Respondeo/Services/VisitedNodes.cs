using System.Text.Json;
using Microsoft.JSInterop;

namespace Respondeo.Services;

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
