using System.Text.Json;
using Markdig;
using Respondeo.SummaImporter;

return SummaImport.Run(args);

// Imports the CCEL plain-text Summa Theologica into the JSON assets the app ships:
//   <output>/summa/summa-index.json              (lightweight browse/search index)
//   <output>/summa/<part-folder>/<id>.json        (full content per question, fetched on demand)
//
// Usage: dotnet run -- <path-to-summa.txt> <output-wwwroot-dir>
//
// Structure: Run validates the arguments and parses the text, then Emit walks the parsed model once,
// writing a content file per question (BuildQuestionContent) while accumulating the browse index (BuildIndexQuestion).
// The anonymous-object shapes here ARE the on-disk JSON contract consumed by SummaService, so their property names and nesting must stay in sync with the app.
internal static class SummaImport
{
    public static int Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: dotnet run -- <path-to-summa.txt> <output-wwwroot-dir>");
            return 1;
        }

        var sourcePath = args[0];
        var outputRoot = args[1];

        if (!File.Exists(sourcePath))
        {
            Console.Error.WriteLine($"Source file not found: {sourcePath}");
            return 1;
        }

        var lines = File.ReadAllLines(sourcePath);
        Console.WriteLine($"Read {lines.Length:N0} lines from {sourcePath}");

        var parts = SummaParser.Parse(lines);
        var summaDir = Path.Combine(outputRoot, "summa");
        var counts = Emit(parts, summaDir);

        Report(parts, summaDir, counts);
        return 0;
    }

    // Walks every part/question once: writes each question's content file and collects the browse index, returning the running totals for the final report.
    private static (int Questions, int Articles) Emit(IReadOnlyList<ParsedPart> parts, string summaDir)
    {
        var renderer = new MarkdownRenderer();
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = false };
        Directory.CreateDirectory(summaDir);

        var indexParts = new List<object>();
        var totalQuestions = 0;
        var totalArticles = 0;

        foreach (var part in parts)
        {
            var indexQuestions = new List<object>();

            foreach (var question in part.Questions)
            {
                totalQuestions++;

                var content = BuildQuestionContent(question, renderer);
                totalArticles += content.Articles.Count;

                WriteQuestionFile(summaDir, question, content, jsonOptions);
                indexQuestions.Add(BuildIndexQuestion(question));
            }

            indexParts.Add(new
            {
                id = part.Id,
                title = part.Title,
                questions = indexQuestions,
            });
        }

        var indexJson = JsonSerializer.Serialize(new { parts = indexParts }, jsonOptions);
        File.WriteAllText(Path.Combine(summaDir, "summa-index.json"), indexJson);

        return (totalQuestions, totalArticles);
    }

    // The full, on-demand-fetched content for a single question: prologue plus every article section rendered to HTML.
    // Articles is materialised so the caller can tally it without re-enumerating.
    private static (object Payload, List<object> Articles) BuildQuestionContent(ParsedQuestion question, MarkdownRenderer renderer)
    {
        var articles = question.Articles.Select(a => (object)new
        {
            number = a.Number,
            title = a.Title,
            preambleHtml = renderer.ToHtml(a.Sections.PreambleMarkdown),
            objections = a.Sections.Objections.Select(o => new { number = o.Number, html = renderer.ToHtml(o.Markdown) }).ToList(),
            sedContraHtml = renderer.ToHtml(a.Sections.SedContraMarkdown ?? string.Empty),
            respondeoHtml = renderer.ToHtml(a.Sections.RespondeoMarkdown ?? string.Empty),
            replies = a.Sections.Replies.Select(r => new { number = r.Number, html = renderer.ToHtml(r.Markdown) }).ToList(),
        }).ToList();

        var payload = new
        {
            id = question.Id,
            partId = question.PartId,
            number = question.Number,
            title = question.Title,
            prologueHtml = renderer.ToHtml(question.PrologueMarkdown),
            articles,
        };

        return (payload, articles);
    }

    // The lightweight browse/search entry for a question: identity, treatise grouping, and article titles only. This is what lands in summa-index.json.
    private static object BuildIndexQuestion(ParsedQuestion question) => new
    {
        id = question.Id,
        number = question.Number,
        title = question.Title,
        treatise = question.Treatise,
        articles = question.Articles.Select(a => new { number = a.Number, title = a.Title }).ToList(),
    };

    private static void WriteQuestionFile(string summaDir, ParsedQuestion question, (object Payload, List<object> Articles) content, JsonSerializerOptions jsonOptions)
    {
        var partDir = Path.Combine(summaDir, PartFolder(question.PartId));
        Directory.CreateDirectory(partDir);

        var contentJson = JsonSerializer.Serialize(content.Payload, jsonOptions);
        File.WriteAllText(Path.Combine(partDir, $"{question.Id}.json"), contentJson);
    }

    // Question content is split into one subfolder per part so a single directory does not hold the entire corpus.
    // The folder name mirrors the human-readable part title in kebab-case; the service derives the same folder from a question's part id when fetching on demand.
    private static string PartFolder(string partId) => partId switch
    {
        "p1" => "first-part",
        "p2a" => "first-part-of-the-second-part",
        "p2b" => "second-part-of-the-second-part",
        "p3" => "third-part",
        "sup" => "supplement",
        _ => partId,
    };

    // Reports the parsed structure so the counts can be validated against known totals.
    private static void Report(IReadOnlyList<ParsedPart> parts, string summaDir, (int Questions, int Articles) counts)
    {
        Console.WriteLine();
        Console.WriteLine("Structure:");
        foreach (var part in parts)
        {
            Console.WriteLine($"  {part.Id,-3} {part.Title,-32} questions={part.Questions.Count,4}");
        }

        Console.WriteLine();
        Console.WriteLine($"Total questions: {counts.Questions:N0}");
        Console.WriteLine($"Total articles:  {counts.Articles:N0}");
        Console.WriteLine($"Index written to: {Path.Combine(summaDir, "summa-index.json")}");
        Console.WriteLine($"Question files:  {summaDir}");
    }
}

// Renders question/article Markdown to HTML with a single shared Markdig pipeline.
// Blank or whitespace-only Markdown maps to an empty string so absent sections stay empty in the JSON.
internal sealed class MarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder().Build();

    public string ToHtml(string markdown) => string.IsNullOrWhiteSpace(markdown) ? string.Empty : Markdown.ToHtml(markdown, _pipeline);
}
