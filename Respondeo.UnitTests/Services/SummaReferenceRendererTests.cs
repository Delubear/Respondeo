using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class SummaReferenceRendererTests
{
    [Fact]
    public void Expand_returns_input_unchanged_when_no_tokens()
    {
        const string html = "<p>Plain prose with no tokens.</p>";

        Assert.Equal(html, SummaReferenceRenderer.Expand(html));
    }

    [Fact]
    public void Expand_handles_null_and_empty()
    {
        Assert.Equal(string.Empty, SummaReferenceRenderer.Expand(null));
        Assert.Equal(string.Empty, SummaReferenceRenderer.Expand(string.Empty));
    }

    [Fact]
    public void Expand_same_part_question_reference()
    {
        var result = SummaReferenceRenderer.Expand("see {{sref|q|fp|3|4}} above");

        Assert.Contains("href=\"summa/fp-q003#article-4\"", result);
        Assert.Contains(">Q. 3, A. 4</a>", result);
    }

    [Fact]
    public void Expand_cross_part_question_reference_shows_part_label()
    {
        var result = SummaReferenceRenderer.Expand("{{sref|qp|fp|88|2}}");

        Assert.Contains("href=\"summa/fp-q088#article-2\"", result);
        Assert.Contains(">FP, Q. 88, A. 2</a>", result);
    }

    [Fact]
    public void Expand_question_reference_without_article_omits_fragment()
    {
        var result = SummaReferenceRenderer.Expand("{{sref|q|ss|1|}}");

        Assert.Contains("href=\"summa/ss-q001\"", result);
        Assert.DoesNotContain("#article-", result);
        Assert.Contains(">Q. 1</a>", result);
    }

    [Fact]
    public void Expand_lone_article_reference_targets_current_question()
    {
        var result = SummaReferenceRenderer.Expand("{{sref|a|tp|12|3}}");

        Assert.Contains("href=\"summa/tp-q012#article-3\"", result);
        Assert.Contains(">A. 3</a>", result);
    }

    [Fact]
    public void Expand_multi_article_reference_renders_a_link_per_article()
    {
        var result = SummaReferenceRenderer.Expand("{{sref|a|fp|14|1,3}}");

        Assert.Contains("href=\"summa/fp-q014#article-1\"", result);
        Assert.Contains(">A. 1</a>", result);
        Assert.Contains("href=\"summa/fp-q014#article-3\"", result);
        Assert.Contains(">A. 3</a>", result);
        Assert.DoesNotContain("{{", result);
    }

    [Fact]
    public void Expand_question_with_multiple_articles_links_each_article()
    {
        var result = SummaReferenceRenderer.Expand("{{sref|q|fp|12|11,12}}");

        Assert.Contains("href=\"summa/fp-q012#article-11\"", result);
        Assert.Contains(">Q. 12, A. 11</a>", result);
        Assert.Contains("href=\"summa/fp-q012#article-12\"", result);
        Assert.Contains(">A. 12</a>", result);
    }

    [Fact]
    public void Expand_cross_part_reference_with_multiple_articles_labels_part_once()
    {
        var result = SummaReferenceRenderer.Expand("{{sref|qp|ss|6|1,4}}");

        Assert.Contains(">SS, Q. 6, A. 1</a>", result);
        Assert.Contains(">A. 4</a>", result);
        // The part label should appear only on the first link.
        Assert.Equal(1, System.Text.RegularExpressions.Regex.Matches(result, "SS, Q\\.").Count);
    }

    [Theory]
    [InlineData("{{scue|objection|1}}", "summa-cue--objection", "Objection 1:")]
    [InlineData("{{scue|reply|2}}", "summa-cue--reply", "Reply to Objection 2:")]
    [InlineData("{{scue|contra}}", "summa-cue--contra", "On the contrary,")]
    [InlineData("{{scue|respondeo}}", "summa-cue--respondeo", "I answer that,")]
    public void Expand_section_cue_tokens(string token, string cssModifier, string label)
    {
        var result = SummaReferenceRenderer.Expand(token);

        Assert.Contains($"class=\"summa-cue {cssModifier}\"", result);
        Assert.Contains($">{label}</strong>", result);
    }

    [Fact]
    public void Expand_handles_reference_and_cue_in_same_html()
    {
        var result = SummaReferenceRenderer.Expand("{{scue|respondeo}} as shown in {{sref|q|fp|2|3}}.");

        Assert.Contains("summa-cue--respondeo", result);
        Assert.Contains("summa-ref", result);
        Assert.DoesNotContain("{{", result);
    }
}
