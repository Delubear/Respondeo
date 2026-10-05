using System.Text.Json;
using Microsoft.JSInterop;

namespace Respondeo.Services;

/// <summary>
/// A <see cref="IBreadcrumbTrail"/> that persists the trail in the browser's <c>sessionStorage</c>.
/// This keeps URLs clean (nothing is added to the query string) while surviving accidental refreshes within the same tab.
/// A shared or deep-linked URL naturally starts a fresh trail.
/// </summary>
public sealed class BreadcrumbTrail(IJSRuntime js) : IBreadcrumbTrail
{
    private const string StorageKey = "respondeo.breadcrumb";
    private const string StageKey = "respondeo.breadcrumb.stage";

    public async Task<IReadOnlyList<string>> VisitAsync(string nodeId, string? stage = null)
    {
        // Each stage is its own journey: when the visitor crosses into a different stage,
        // start the trail over so breadcrumbs don't carry nodes from the previous section.
        var previousStage = await js.InvokeAsync<string?>("sessionStorage.getItem", StageKey);
        var trail = string.Equals(previousStage ?? string.Empty, stage ?? string.Empty, StringComparison.OrdinalIgnoreCase) ? await LoadAsync() : [];

        var index = trail.IndexOf(nodeId);
        if (index >= 0)
        {
            // Revisiting an earlier node: truncate back to it (handles back-nav and loops).
            trail.RemoveRange(index + 1, trail.Count - index - 1);
        }
        else
        {
            trail.Add(nodeId);
        }

        await SaveAsync(trail);
        await js.InvokeVoidAsync("sessionStorage.setItem", StageKey, stage ?? string.Empty);
        return trail;
    }

    public async Task ClearAsync()
    {
        await js.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
        await js.InvokeVoidAsync("sessionStorage.removeItem", StageKey);
    }

    private async Task<List<string>> LoadAsync()
    {
        try
        {
            var json = await js.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);
            return string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task SaveAsync(List<string> trail)
    {
        var json = JsonSerializer.Serialize(trail);
        await js.InvokeVoidAsync("sessionStorage.setItem", StorageKey, json);
    }
}
