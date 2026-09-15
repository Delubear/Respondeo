using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Respondeo.Services;

/// <summary>
/// Renders a small vocabulary of authoring directives so content authors can embed media without hand-writing HTML.
/// Each directive expands to the canonical markup (and CSS classes) in one place, keeping embeds consistent as content grows.
///
/// Usage in Markdown (custom-container syntax, enabled by UseAdvancedExtensions):
///   ::: youtube aqz-KE-bpKQ
///   :::
///   ::: pdf content/assets/sample.pdf
///   :::
///   ::: button content/assets/sample.pdf | Open PDF in new tab
///   :::
///   ::: button content/assets/sample.pdf | Download PDF | download
///   :::
///
/// Any other container name falls back to Markdig's default &lt;div&gt; rendering.
/// </summary>
public sealed class ContentContainerExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is not HtmlRenderer htmlRenderer)
        {
            return;
        }

        var existing = htmlRenderer.ObjectRenderers.OfType<HtmlCustomContainerRenderer>().FirstOrDefault();
        if (existing is not null)
        {
            htmlRenderer.ObjectRenderers.Remove(existing);
        }

        htmlRenderer.ObjectRenderers.Add(new ContentContainerRenderer());
    }
}

/// <summary>
/// Expands known content directives to canonical HTML; delegates unknown ones to the standard custom-container rendering.
/// </summary>
internal sealed class ContentContainerRenderer : HtmlObjectRenderer<CustomContainer>
{
    protected override void Write(HtmlRenderer renderer, CustomContainer obj)
    {
        var info = obj.Info?.Trim();
        var argument = obj.Arguments?.Trim() ?? string.Empty;

        switch (info)
        {
            case "youtube":
                WriteYouTube(renderer, argument);
                return;
            case "pdf":
                WritePdf(renderer, argument);
                return;
            case "button":
                WriteButton(renderer, argument);
                return;
            default:
                WriteDefault(renderer, obj);
                return;
        }
    }

    private static void WriteYouTube(HtmlRenderer renderer, string videoId)
    {
        renderer.EnsureLine();
        renderer.Write("<div class=\"video-embed\">");
        renderer.Write("<iframe src=\"https://www.youtube-nocookie.com/embed/");
        renderer.WriteEscapeUrl(videoId);
        renderer.Write("\" title=\"Embedded YouTube video\" loading=\"lazy\" ");
        renderer.Write("allow=\"accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share\" ");
        renderer.Write("referrerpolicy=\"strict-origin-when-cross-origin\" allowfullscreen></iframe>");
        renderer.WriteLine("</div>");
    }

    private static void WritePdf(HtmlRenderer renderer, string path)
    {
        renderer.EnsureLine();
        renderer.Write("<div class=\"pdf-embed\">");
        renderer.Write("<iframe src=\"");
        renderer.WriteEscapeUrl(path);
        renderer.Write("\" title=\"Embedded PDF document\" loading=\"lazy\"></iframe>");
        renderer.WriteLine("</div>");
    }

    private static void WriteButton(HtmlRenderer renderer, string argument)
    {
        var parts = argument.Split('|', 3);
        var href = parts[0].Trim();
        var text = parts.Length > 1 ? parts[1].Trim() : href;
        var isDownload = parts.Length > 2 && parts[2].Trim().Equals("download", StringComparison.OrdinalIgnoreCase);

        renderer.EnsureLine();
        renderer.Write("<div class=\"test-actions\">");
        renderer.Write("<a class=\"btn\" href=\"");
        renderer.WriteEscapeUrl(href);
        renderer.Write(isDownload ? "\" download>" : "\" target=\"_blank\" rel=\"noopener noreferrer\">");
        renderer.WriteEscape(text);
        renderer.Write("</a>");
        renderer.WriteLine("</div>");
    }

    private static void WriteDefault(HtmlRenderer renderer, CustomContainer obj)
    {
        renderer.EnsureLine();
        renderer.Write("<div").WriteAttributes(obj).Write('>');
        renderer.WriteChildren(obj);
        renderer.WriteLine("</div>");
    }
}
