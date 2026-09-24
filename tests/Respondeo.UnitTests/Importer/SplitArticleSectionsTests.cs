using Respondeo.SummaImporter;

namespace Respondeo.UnitTests.Importer;

/// <summary>
/// Unit tests for <see cref="SummaParser.SplitArticleSections"/>, which partitions an already-linkified
/// article body (Markdown paragraphs joined by blank lines, each classic section paragraph carrying a
/// leading <c>{{scue|...}}</c> token) into preamble/objections/sedContra/respondeo/replies buckets.
/// The tokens must be preserved inside each bucket so the render stage still emits the correct labels
/// and deep-link anchor ids.
/// </summary>
public class SplitArticleSectionsTests
{
    private static string Join(params string[] paragraphs) => string.Join("\n\n", paragraphs);

    [Fact]
    public void Splits_a_full_article_into_its_sections()
    {
        var body = Join(
            "{{scue|objection|1}} It seems not.",
            "{{scue|objection|2}} Further, it seems not.",
            "{{scue|contra}} On the authority of Scripture.",
            "{{scue|respondeo}} I answer that it is so.",
            "{{scue|reply|1}} The first objection fails.",
            "{{scue|reply|2}} The second objection fails.");

        var sections = SummaParser.SplitArticleSections(body);

        Assert.Empty(sections.PreambleMarkdown);
        Assert.Equal(2, sections.Objections.Count);
        Assert.Equal(1, sections.Objections[0].Number);
        Assert.Equal(2, sections.Objections[1].Number);
        Assert.NotNull(sections.SedContraMarkdown);
        Assert.NotNull(sections.RespondeoMarkdown);
        Assert.Equal(2, sections.Replies.Count);
        Assert.Equal(1, sections.Replies[0].Number);
        Assert.Equal(2, sections.Replies[1].Number);
    }

    [Fact]
    public void Preserves_the_scue_tokens_inside_each_bucket()
    {
        var body = Join(
            "{{scue|objection|1}} It seems not.",
            "{{scue|contra}} On the contrary.",
            "{{scue|respondeo}} I answer that.",
            "{{scue|reply|1}} Reply text.");

        var sections = SummaParser.SplitArticleSections(body);

        Assert.StartsWith("{{scue|objection|1}}", sections.Objections[0].Markdown);
        Assert.StartsWith("{{scue|contra}}", sections.SedContraMarkdown);
        Assert.StartsWith("{{scue|respondeo}}", sections.RespondeoMarkdown);
        Assert.StartsWith("{{scue|reply|1}}", sections.Replies[0].Markdown);
    }

    [Fact]
    public void Attaches_cueless_continuation_paragraphs_to_the_open_section()
    {
        var body = Join(
            "{{scue|respondeo}} First paragraph of the answer.",
            "Second paragraph of the answer.",
            "Third paragraph of the answer.");

        var sections = SummaParser.SplitArticleSections(body);

        Assert.NotNull(sections.RespondeoMarkdown);
        Assert.Contains("First paragraph", sections.RespondeoMarkdown);
        Assert.Contains("Second paragraph", sections.RespondeoMarkdown);
        Assert.Contains("Third paragraph", sections.RespondeoMarkdown);
    }

    [Fact]
    public void Puts_text_before_the_first_cue_into_the_preamble()
    {
        var body = Join(
            "Some lead-in prose before the objections.",
            "{{scue|objection|1}} It seems not.");

        var sections = SummaParser.SplitArticleSections(body);

        Assert.Contains("lead-in prose", sections.PreambleMarkdown);
        Assert.Single(sections.Objections);
    }

    [Fact]
    public void Handles_an_article_with_no_sed_contra()
    {
        var body = Join(
            "{{scue|objection|1}} It seems not.",
            "{{scue|respondeo}} I answer that.",
            "{{scue|reply|1}} Reply text.");

        var sections = SummaParser.SplitArticleSections(body);

        Assert.Null(sections.SedContraMarkdown);
        Assert.NotNull(sections.RespondeoMarkdown);
        Assert.Single(sections.Objections);
        Assert.Single(sections.Replies);
    }

    [Fact]
    public void Handles_a_body_with_no_cues_as_a_single_preamble()
    {
        var body = Join(
            "A single-paragraph article body.",
            "With a second paragraph and no cues.");

        var sections = SummaParser.SplitArticleSections(body);

        Assert.Contains("single-paragraph", sections.PreambleMarkdown);
        Assert.Contains("second paragraph", sections.PreambleMarkdown);
        Assert.Empty(sections.Objections);
        Assert.Empty(sections.Replies);
        Assert.Null(sections.SedContraMarkdown);
        Assert.Null(sections.RespondeoMarkdown);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Handles_empty_or_null_body(string? body)
    {
        var sections = SummaParser.SplitArticleSections(body!);

        Assert.Empty(sections.PreambleMarkdown);
        Assert.Empty(sections.Objections);
        Assert.Empty(sections.Replies);
        Assert.Null(sections.SedContraMarkdown);
        Assert.Null(sections.RespondeoMarkdown);
    }
}
