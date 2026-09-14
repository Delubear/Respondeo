using System.Text.Json;
using Microsoft.JSInterop;

namespace Respondeo.Services;

/// <summary>
/// A <see cref="IBreadcrumbTrail"/> that persists the trail in the browser's <c>sessionStorage</c>.
/// This keeps URLs clean (nothing is added to the query string) while surviving accidental refreshes within the same tab.
/// A shared or deep-linked URL naturally starts a fresh trail.
/// </summary>
public sealed class BreadcrumbTrail : IBreadcrumbTrail
{
    private const string StorageKey = "respondeo.breadcrumb";

    private readonly IJSRuntime _js;

    public BreadcrumbTrail(IJSRuntime js) => _js = js;

    public async Task<IReadOnlyList<string>> VisitAsync(string nodeId)
    {
        var trail = await LoadAsync();

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
        return trail;
    }

    private async Task<List<string>> LoadAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<string>();
            }

            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }

    private async Task SaveAsync(List<string> trail)
    {
        var json = JsonSerializer.Serialize(trail);
        await _js.InvokeVoidAsync("sessionStorage.setItem", StorageKey, json);
    }
}
