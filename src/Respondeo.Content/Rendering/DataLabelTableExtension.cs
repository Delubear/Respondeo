using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Respondeo.Content.Rendering;

/// <summary>
/// Replaces Markdig's default table renderer with one that stamps every body cell with a
/// <c>data-label</c> attribute carrying its column header text. This lets the stylesheet reflow
/// tables into stacked, labelled cards on narrow screens (where a side-by-side grid would be
/// unreadable) without the content pipeline or authors having to hand-write any markup.
/// </summary>
internal sealed class DataLabelTableExtension : IMarkdownExtension
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

        var existing = htmlRenderer.ObjectRenderers.OfType<HtmlTableRenderer>().FirstOrDefault();
        if (existing is not null)
        {
            htmlRenderer.ObjectRenderers.Remove(existing);
        }

        htmlRenderer.ObjectRenderers.Add(new DataLabelTableRenderer());
    }
}

/// <summary>
/// Renders a pipe table to semantic <c>&lt;thead&gt;</c>/<c>&lt;tbody&gt;</c> HTML, honouring column
/// alignment and adding a <c>data-label</c> on each body cell so CSS can present the table as
/// stacked cards on mobile.
/// </summary>
internal sealed class DataLabelTableRenderer : HtmlObjectRenderer<Table>
{
    protected override void Write(HtmlRenderer renderer, Table table)
    {
        var headerLabels = ExtractHeaderLabels(renderer, table);

        renderer.EnsureLine();
        renderer.WriteLine("<table>");

        var wroteHeadOpen = false;
        var wroteBodyOpen = false;

        foreach (var rowObj in table)
        {
            var row = (TableRow)rowObj;

            if (row.IsHeader)
            {
                if (!wroteHeadOpen)
                {
                    renderer.WriteLine("<thead>");
                    wroteHeadOpen = true;
                }
            }
            else
            {
                if (wroteHeadOpen)
                {
                    renderer.WriteLine("</thead>");
                    wroteHeadOpen = false;
                }

                if (!wroteBodyOpen)
                {
                    renderer.WriteLine("<tbody>");
                    wroteBodyOpen = true;
                }
            }

            renderer.WriteLine("<tr>");

            for (var i = 0; i < row.Count; i++)
            {
                var cell = (TableCell)row[i];
                var columnIndex = cell.ColumnIndex >= 0 ? cell.ColumnIndex : i;
                var tag = row.IsHeader ? "th" : "td";

                renderer.Write('<').Write(tag);
                WriteAlignment(renderer, table, columnIndex);

                if (!row.IsHeader && columnIndex < headerLabels.Count && headerLabels[columnIndex].Length > 0)
                {
                    renderer.Write(" data-label=\"");
                    renderer.WriteEscape(headerLabels[columnIndex]);
                    renderer.Write('"');
                }

                renderer.Write('>');
                renderer.Write(RenderCellInnerHtml(renderer, cell).Trim());
                renderer.Write("</").Write(tag).WriteLine('>');
            }

            renderer.WriteLine("</tr>");
        }

        if (wroteHeadOpen)
        {
            renderer.WriteLine("</thead>");
        }

        if (wroteBodyOpen)
        {
            renderer.WriteLine("</tbody>");
        }

        renderer.WriteLine("</table>");
    }

    private static void WriteAlignment(HtmlRenderer renderer, Table table, int columnIndex)
    {
        if (columnIndex >= table.ColumnDefinitions.Count)
        {
            return;
        }

        var alignment = table.ColumnDefinitions[columnIndex].Alignment;
        var css = alignment switch
        {
            TableColumnAlign.Center => "text-align: center;",
            TableColumnAlign.Right => "text-align: right;",
            TableColumnAlign.Left => "text-align: left;",
            _ => null,
        };

        if (css is not null)
        {
            renderer.Write(" style=\"").Write(css).Write('"');
        }
    }

    private List<string> ExtractHeaderLabels(HtmlRenderer renderer, Table table)
    {
        var labels = new List<string>();

        var headerRow = table.OfType<TableRow>().FirstOrDefault(r => r.IsHeader);
        if (headerRow is null)
        {
            return labels;
        }

        foreach (var cellObj in headerRow)
        {
            var cell = (TableCell)cellObj;
            var html = RenderCellInnerHtml(renderer, cell);
            labels.Add(StripTags(html).Trim());
        }

        return labels;
    }

    /// <summary>
    /// Renders a cell's inline content to HTML using the active renderer, with tag output temporarily
    /// toggled on, then restores the renderer's writer so the caller's output stream is untouched.
    /// </summary>
    private string RenderCellInnerHtml(HtmlRenderer renderer, TableCell cell)
    {
        var originalWriter = renderer.Writer;
        var originalEnableHtml = renderer.EnableHtmlForInline;

        using var buffer = new StringWriter();
        renderer.Writer = buffer;
        renderer.EnableHtmlForInline = true;

        try
        {
            renderer.WriteChildren(cell);
        }
        finally
        {
            renderer.Writer = originalWriter;
            renderer.EnableHtmlForInline = originalEnableHtml;
        }

        return buffer.ToString();
    }

    private static readonly Regex TagRegex = new("<[^>]+>", RegexOptions.Compiled);

    private static string StripTags(string html) => TagRegex.Replace(html, string.Empty);
}
