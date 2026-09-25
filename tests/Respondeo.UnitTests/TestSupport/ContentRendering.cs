using Respondeo.Content.Abstractions;
using Respondeo.Content.Rendering;

namespace Respondeo.UnitTests.TestSupport;

/// <summary>
/// Provides the real, shared <see cref="IContentHtmlRenderer"/> implementation for tests that construct
/// parsers or services directly. Using the production Markdig-backed renderer keeps the rendered HTML in
/// tests identical to what the running app produces.
/// </summary>
internal static class ContentRendering
{
    public static IContentHtmlRenderer Renderer { get; } = new MarkdigContentHtmlRenderer();
}
