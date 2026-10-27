using Microsoft.JSInterop;

namespace Respondeo.Services;

/// <summary>
/// The available reading text sizes for the app.
/// </summary>
public enum TextSize
{
    Default,
    Large,
    Larger,
    Largest,
}

/// <summary>
/// Reads and applies the reader's preferred <see cref="TextSize"/>, persisting the choice across sessions.
/// </summary>
public interface ITextSizeService
{
    /// <summary>
    /// Gets the currently applied text size. Defaults to <see cref="TextSize.Default"/> until initialized.
    /// </summary>
    TextSize Current { get; }

    /// <summary>
    /// Reads the persisted preference and applies it to the document.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Advances to the next text size (wrapping back to the default), persists it, and applies it to the document.
    /// </summary>
    Task CycleAsync();

    /// <summary>
    /// Sets the given text size, persists it, and applies it to the document.
    /// </summary>
    Task SetAsync(TextSize size);
}

/// <summary>
/// An <see cref="ITextSizeService"/> that persists the choice in the browser's <c>localStorage</c>
/// and applies it by setting <c>data-textsize</c> on the document root, which scales article/page content only.
/// </summary>
public sealed class TextSizeService(IJSRuntime js) : ITextSizeService
{
    private const string StorageKey = "respondeo.textsize";
    private readonly LocalStorage _storage = new(js);

    public TextSize Current { get; private set; } = TextSize.Default;

    public async Task InitializeAsync()
    {
        var stored = await _storage.GetAsync(StorageKey);

        Current = stored switch
        {
            "large" => TextSize.Large,
            "larger" => TextSize.Larger,
            "largest" => TextSize.Largest,
            _ => TextSize.Default,
        };

        await ApplyAsync();
    }

    public Task CycleAsync() => SetAsync(Current == TextSize.Largest ? TextSize.Default : Current + 1);

    public async Task SetAsync(TextSize size)
    {
        Current = size;
        await _storage.SetAsync(StorageKey, Name(Current));
        await ApplyAsync();
    }

    private async Task ApplyAsync() => await js.InvokeVoidAsync("respondeoTextSize.apply", Name(Current));

    private static string Name(TextSize size) => size.ToString().ToLowerInvariant();
}
