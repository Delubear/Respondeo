using Markdig;
using Respondeo.Content.Rendering;

namespace Respondeo.UnitTests.Services;

public class DataLabelTableExtensionTests
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Use<DataLabelTableExtension>()
        .Build();

    private const string SampleTable =
        "| Example | What is it? | Who is it? |\n" +
        "|---|---|:---:|\n" +
        "| A tree | one tree-nature | **0** |\n" +
        "| God | one divine nature | three |\n";

    private static string Render(string markdown) => Markdown.ToHtml(markdown, Pipeline);

    [Fact]
    public void Table_renders_semantic_thead_and_tbody()
    {
        var html = Render(SampleTable);

        Assert.Contains("<thead>", html);
        Assert.Contains("<tbody>", html);
        Assert.Contains("<th", html);
    }

    [Fact]
    public void Body_cells_carry_data_label_from_column_header()
    {
        var html = Render(SampleTable);

        Assert.Contains("data-label=\"Example\"", html);
        Assert.Contains("data-label=\"What is it?\"", html);
        Assert.Contains("data-label=\"Who is it?\"", html);
    }

    [Fact]
    public void Header_cells_do_not_carry_data_label()
    {
        var html = Render(SampleTable);

        var theadEnd = html.IndexOf("</thead>", StringComparison.Ordinal);
        var headerHtml = html[..theadEnd];

        Assert.DoesNotContain("data-label", headerHtml);
    }

    [Fact]
    public void Column_alignment_is_preserved_as_inline_style()
    {
        var html = Render(SampleTable);

        Assert.Contains("text-align: center;", html);
    }

    [Fact]
    public void Inline_markdown_inside_cells_is_still_rendered()
    {
        var html = Render(SampleTable);

        Assert.Contains("<strong>0</strong>", html);
    }
}
