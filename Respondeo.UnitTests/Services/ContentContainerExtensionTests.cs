using Markdig;
using Respondeo.Content.Markdown.Services;

namespace Respondeo.UnitTests.Services;

public class ContentContainerExtensionTests
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Use<ContentContainerExtension>()
        .Build();

    private static string Render(string markdown) => Markdown.ToHtml(markdown, Pipeline);

    [Fact]
    public void Youtube_directive_renders_nocookie_iframe()
    {
        var html = Render("::: youtube aqz-KE-bpKQ\n:::");

        Assert.Contains("class=\"video-embed\"", html);
        Assert.Contains("https://www.youtube-nocookie.com/embed/aqz-KE-bpKQ", html);
        Assert.Contains("allowfullscreen", html);
    }

    [Fact]
    public void Pdf_directive_renders_pdf_embed()
    {
        var html = Render("::: pdf content/assets/sample.pdf\n:::");

        Assert.Contains("class=\"pdf-embed\"", html);
        Assert.Contains("src=\"content/assets/sample.pdf\"", html);
    }

    [Fact]
    public void Button_directive_uses_href_as_text_when_label_omitted()
    {
        var html = Render("::: button content/assets/sample.pdf\n:::");

        Assert.Contains("class=\"test-actions\"", html);
        Assert.Contains("class=\"btn\"", html);
        Assert.Contains("target=\"_blank\"", html);
        Assert.Contains("rel=\"noopener noreferrer\"", html);
        Assert.Contains(">content/assets/sample.pdf</a>", html);
    }

    [Fact]
    public void Button_directive_uses_custom_label()
    {
        var html = Render("::: button content/assets/sample.pdf | Open PDF\n:::");

        Assert.Contains(">Open PDF</a>", html);
        Assert.DoesNotContain("download", html);
    }

    [Fact]
    public void Button_directive_download_variant_adds_download_attribute()
    {
        var html = Render("::: button content/assets/sample.pdf | Download PDF | download\n:::");

        Assert.Contains("download>", html);
        Assert.Contains(">Download PDF</a>", html);
        Assert.DoesNotContain("target=\"_blank\"", html);
    }

    [Fact]
    public void Unknown_directive_falls_back_to_default_div()
    {
        var html = Render("::: sidebar\nHello\n:::");

        Assert.Contains("<div", html);
        Assert.Contains("Hello", html);
    }
}
