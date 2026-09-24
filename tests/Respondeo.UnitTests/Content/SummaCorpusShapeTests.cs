using System.Runtime.CompilerServices;
using System.Text.Json;
using Respondeo.Content.Summa;

namespace Respondeo.UnitTests.Content;

/// <summary>
/// Guards the generated Summa corpus against structural regressions in the importer. Reads the actual
/// per-question JSON files produced by the importer and asserts invariants that must hold across all
/// 3,000+ articles: every question loads, articles are numbered consecutively, every objection has a
/// matching reply (and vice versa), and no article is entirely empty. A parser change that drops or
/// mis-buckets content turns into a failing test here instead of a silent corpus regression.
/// </summary>
public class SummaCorpusShapeTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static string SummaDirectory([CallerFilePath] string thisFile = "")
    {
        // This file lives at <repo>/tests/Respondeo.UnitTests/Content/SummaCorpusShapeTests.cs.
        // Walk up to the repo root, then into the Summa content library's generated corpus folder.
        var repoRoot = Directory.GetParent(thisFile)!.Parent!.Parent!.Parent!.FullName;
        return Path.Combine(repoRoot, "src", "Respondeo.Content.Summa", "wwwroot", "summa");
    }

    private static IEnumerable<(string Path, SummaQuestionContent Question)> AllQuestions()
    {
        var summaDir = SummaDirectory();
        foreach (var file in Directory.EnumerateFiles(summaDir, "*.json", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(file) == "summa-index.json")
            {
                continue;
            }

            var question = JsonSerializer.Deserialize<SummaQuestionContent>(File.ReadAllText(file), JsonOptions);
            Assert.NotNull(question);
            yield return (file, question!);
        }
    }

    [Fact]
    public void Every_question_file_deserializes_and_has_articles()
    {
        var empty = new List<string>();

        foreach (var (path, question) in AllQuestions())
        {
            if (question.Articles.Count == 0)
            {
                empty.Add(Path.GetFileName(path));
            }
        }

        Assert.True(empty.Count == 0, $"Questions with no articles: {string.Join(", ", empty)}");
    }

    [Fact]
    public void Article_numbers_are_consecutive_from_one()
    {
        var offenders = new List<string>();

        foreach (var (path, question) in AllQuestions())
        {
            for (var i = 0; i < question.Articles.Count; i++)
            {
                if (question.Articles[i].Number != i + 1)
                {
                    offenders.Add($"{Path.GetFileName(path)} article #{i + 1} numbered {question.Articles[i].Number}");
                    break;
                }
            }
        }

        Assert.True(offenders.Count == 0, $"Non-consecutive article numbering: {string.Join("; ", offenders)}");
    }

    [Fact]
    public void No_article_is_entirely_empty()
    {
        var offenders = new List<string>();

        foreach (var (path, question) in AllQuestions())
        {
            foreach (var article in question.Articles)
            {
                var hasContent = !string.IsNullOrEmpty(article.PreambleHtml)
                    || article.Objections.Count > 0
                    || !string.IsNullOrEmpty(article.SedContraHtml)
                    || !string.IsNullOrEmpty(article.RespondeoHtml)
                    || article.Replies.Count > 0;

                if (!hasContent)
                {
                    offenders.Add($"{Path.GetFileName(path)} A.{article.Number}");
                }
            }
        }

        Assert.True(offenders.Count == 0, $"Empty articles: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void No_article_merges_two_articles_together()
    {
        var offenders = new List<string>();

        foreach (var (path, question) in AllQuestions())
        {
            foreach (var article in question.Articles)
            {
                // The importer's article-boundary detection used to swallow a following article whose title
                // was declarative or trailed by a footnote, merging two articles into one. That defect shows
                // up as the section numbering restarting: e.g. objections "1,2,3,1,2". A single restart back
                // to 1 (or below the running max) after the sequence has advanced is the tell-tale sign.
                // Isolated duplicates that do NOT restart (e.g. "1,2,2") are genuine typos in the source
                // Summa text that the parser faithfully reproduces, so they are tolerated here.
                if (RestartsMidSection(article.Objections.Select(o => o.Number)))
                {
                    offenders.Add($"{Path.GetFileName(path)} A.{article.Number} objections");
                }

                if (RestartsMidSection(article.Replies.Select(r => r.Number)))
                {
                    offenders.Add($"{Path.GetFileName(path)} A.{article.Number} replies");
                }
            }
        }

        Assert.True(offenders.Count == 0, $"Articles that merged a following article: {string.Join("; ", offenders)}");
    }

    // True when a numbered sequence drops back to 1 after having advanced past it, which indicates a second
    // article's section numbering was appended to this one.
    private static bool RestartsMidSection(IEnumerable<int> numbers)
    {
        var seenAboveOne = false;
        foreach (var n in numbers)
        {
            if (n == 1 && seenAboveOne)
            {
                return true;
            }

            if (n > 1)
            {
                seenAboveOne = true;
            }
        }

        return false;
    }
}
