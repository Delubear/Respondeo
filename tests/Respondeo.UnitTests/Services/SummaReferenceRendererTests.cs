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
        var result = SummaReferenceRenderer.Expand("see {{sref|q|p1|3|4}} above");

        Assert.Contains("href=\"summa/prima-q003#article-4\"", result);
        Assert.Contains(">Q. 3, A. 4</a>", result);
    }

    [Fact]
    public void Expand_cross_part_question_reference_shows_part_label()
    {
        var result = SummaReferenceRenderer.Expand("{{sref|qp|p1|88|2}}");

        Assert.Contains("href=\"summa/prima-q088#article-2\"", result);
        Assert.Contains(">I, Q. 88, A. 2</a>", result);
    }

    [Fact]
    public void Expand_question_reference_without_article_omits_fragment()
    {
        var result = SummaReferenceRenderer.Expand("{{sref|q|p2b|1|}}");

        Assert.Contains("href=\"summa/secsec-q001\"", result);
        Assert.DoesNotContain("#article-", result);
        Assert.Contains(">Q. 1</a>", result);
    }

    [Fact]
    public void Expand_lone_article_reference_targets_current_question()
    {
        var result = SummaReferenceRenderer.Expand("{{sref|a|p3|12|3}}");

        Assert.Contains("href=\"summa/tertia-q012#article-3\"", result);
        Assert.Contains(">A. 3</a>", result);
    }

    [Fact]
    public void Expand_multi_article_reference_renders_a_link_per_article()
    {
        var result = SummaReferenceRenderer.Expand("{{sref|a|p1|14|1,3}}");

        Assert.Contains("href=\"summa/prima-q014#article-1\"", result);
        Assert.Contains(">A. 1</a>", result);
        Assert.Contains("href=\"summa/prima-q014#article-3\"", result);
        Assert.Contains(">A. 3</a>", result);
        Assert.DoesNotContain("{{", result);
    }

    [Fact]
    public void Expand_question_with_multiple_articles_links_each_article()
    {
        var result = SummaReferenceRenderer.Expand("{{sref|q|p1|12|11,12}}");

        Assert.Contains("href=\"summa/prima-q012#article-11\"", result);
        Assert.Contains(">Q. 12, A. 11</a>", result);
        Assert.Contains("href=\"summa/prima-q012#article-12\"", result);
        Assert.Contains(">A. 12</a>", result);
    }

    [Fact]
    public void Expand_cross_part_reference_with_multiple_articles_labels_part_once()
    {
        var result = SummaReferenceRenderer.Expand("{{sref|qp|p2b|6|1,4}}");

        Assert.Contains(">II-II, Q. 6, A. 1</a>", result);
        Assert.Contains(">A. 4</a>", result);
        // The part label should appear only on the first link.
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(result, "II-II, Q\\."));
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
        var result = SummaReferenceRenderer.Expand("{{scue|respondeo}} as shown in {{sref|q|p1|2|3}}.");

        Assert.Contains("summa-cue--respondeo", result);
        Assert.Contains("summa-ref", result);
        Assert.DoesNotContain("{{", result);
    }

    [Fact]
    public void Expand_objection_reference_links_to_objection_fragment()
    {
        var result = SummaReferenceRenderer.Expand("{{sref|a|p2a|8|1}}{{sobj|p2a|8|1|objection|3}}");

        Assert.Contains("href=\"summa/primsec-q008#article-1-objection-3\"", result);
        Assert.Contains(">obj.&nbsp;3</a>", result);
        Assert.DoesNotContain("{{", result);
    }

    [Fact]
    public void Expand_reply_objection_reference_links_to_reply_fragment()
    {
        var result = SummaReferenceRenderer.Expand("{{sref|qp|p3|31|6}}{{sobj|p3|31|6|reply|1}}");

        Assert.Contains("href=\"summa/tertia-q031#article-6-reply-1\"", result);
        Assert.Contains(">ad&nbsp;1</a>", result);
    }

    [Fact]
    public void Expand_combined_same_part_citation_renders_single_reply_link()
    {
        var result = SummaReferenceRenderer.Expand("see {{scite|q|p2a|13|1|reply|2}} above");

        Assert.Contains("href=\"summa/primsec-q013#article-1-reply-2\"", result);
        Assert.Contains(">Q.&nbsp;13, A.&nbsp;1, ad&nbsp;2</a>", result);
        Assert.DoesNotContain("{{", result);
    }

    [Fact]
    public void Expand_combined_cross_part_citation_shows_part_label()
    {
        var result = SummaReferenceRenderer.Expand("{{scite|qp|p2b|80|1|objection|4}}");

        Assert.Contains("href=\"summa/secsec-q080#article-1-objection-4\"", result);
        Assert.Contains(">II-II, Q.&nbsp;80, A.&nbsp;1, obj.&nbsp;4</a>", result);
    }

    [Fact]
    public void Expand_combined_article_only_citation_omits_question_label()
    {
        var result = SummaReferenceRenderer.Expand("{{scite|a|p1|29|3|reply|2}}");

        Assert.Contains("href=\"summa/prima-q029#article-3-reply-2\"", result);
        Assert.Contains(">A.&nbsp;3, ad&nbsp;2</a>", result);
    }

    [Fact]
    public void Expand_self_citation_renders_bare_reply_link()
    {
        var result = SummaReferenceRenderer.Expand("({{scite|self|p2a|48|3|reply|1}})");

        Assert.Contains("href=\"summa/primsec-q048#article-3-reply-1\"", result);
        Assert.Contains(">ad&nbsp;1</a>", result);
        Assert.DoesNotContain("Q.&nbsp;", result);
    }

    [Fact]
    public void Expand_objection_cue_emits_anchor_id_when_article_known()
    {
        var result = SummaReferenceRenderer.Expand("{{scue|objection|2}}It seems", 5);

        Assert.Contains("id=\"article-5-objection-2\"", result);
    }

    [Fact]
    public void Expand_reply_cue_emits_anchor_id_when_article_known()
    {
        var result = SummaReferenceRenderer.Expand("{{scue|reply|1}}On the contrary", 4);

        Assert.Contains("id=\"article-4-reply-1\"", result);
    }

    [Fact]
    public void Expand_objection_cue_omits_id_without_article_context()
    {
        var result = SummaReferenceRenderer.Expand("{{scue|objection|2}}It seems");

        Assert.DoesNotContain("id=\"article-", result);
    }

    [Fact]
    public void Expand_contra_cue_emits_numberless_anchor_id_when_article_known()
    {
        var result = SummaReferenceRenderer.Expand("{{scue|contra}}the Philosopher", 5);

        Assert.Contains("id=\"article-5-contra\"", result);
    }

    [Fact]
    public void Expand_respondeo_cue_emits_numberless_anchor_id_when_article_known()
    {
        var result = SummaReferenceRenderer.Expand("{{scue|respondeo}}it must be said", 3);

        Assert.Contains("id=\"article-3-respondeo\"", result);
    }

    [Fact]
    public void Expand_contra_cue_omits_id_without_article_context()
    {
        var result = SummaReferenceRenderer.Expand("{{scue|contra}}the Philosopher");

        Assert.DoesNotContain("id=\"article-", result);
    }
}
