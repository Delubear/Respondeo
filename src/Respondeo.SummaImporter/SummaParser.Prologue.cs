using System.Text.RegularExpressions;

namespace Respondeo.SummaImporter;

// Prologue clean-up: trimming the enumerated article list that duplicates the on-page table of contents
// while preserving the lead-in sentence and every other prologue paragraph.
internal static partial class SummaParser
{
    // Regex for the numbered inquiry-point paragraphs at the end of a question prologue, e.g. "(1) Whether God is a body?".
    // These enumerate the articles that follow and are reproduced by the on-page table of contents, so they are trimmed to avoid duplicating that list.
    [GeneratedRegex(@"^\(\d+\)\s", RegexOptions.Compiled)]
    private static partial Regex InquiryPointRegex();

    // Regex for the lead-in sentence that introduces the article enumeration.
    // It ends in a colon or a period and either mentions "inquiry/inquire" or enumerates a number of "points",
    // e.g. "... eight points of inquiry:", "... three subjects of inquiry:", "... three points for treatment:", or "Concerning evil, six points are to be considered:".
    // Combined with the requirement that an enumerated "(N) ..." paragraph follows,
    // this targets the article table of contents while leaving structural treatise-division lists (which say only "... consider X:") untouched.
    [GeneratedRegex(@"(\binquir(y|e|ies)\b|\bpoints?\b)[^.:<]*[.:]$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex InquiryLeadInRegex();

    // Drops the enumerated inquiry list from a prologue while keeping the lead-in sentence that introduces it (e.g. "... there are eight points of inquiry:"),
    // so it reads as an intro to the on-page article-links list.
    // Every other paragraph - structural division lists before it and notes trailing after it - is preserved.
    // The numbered items themselves duplicate the table of contents.
    private static string TrimInquiryList(string prologue)
    {
        if (string.IsNullOrEmpty(prologue))
        {
            return prologue;
        }

        var paragraphs = prologue.Split("\n\n").ToList();

        // The article enumeration is always introduced by a lead-in sentence mentioning "points of inquiry",
        // e.g. "... there are eight points of inquiry:" or "... four points of inquiry arise:".
        // Require it to be immediately followed by an enumerated "(N) ..." paragraph so incidental mentions of inquiry,
        // and structural treatise-division lists, are left untouched.
        var leadIn = -1;
        for (var i = 0; i < paragraphs.Count - 1; i++)
        {
            if (InquiryLeadInRegex().IsMatch(paragraphs[i].TrimEnd()) &&
                InquiryPointRegex().IsMatch(paragraphs[i + 1].TrimStart()))
            {
                leadIn = i;
                break;
            }
        }

        if (leadIn < 0)
        {
            return prologue;
        }

        // Remove the contiguous block of enumerated "(N) ..." paragraphs after the lead-in, keeping the lead-in itself.
        var blockEnd = leadIn;
        while (blockEnd + 1 < paragraphs.Count && InquiryPointRegex().IsMatch(paragraphs[blockEnd + 1].TrimStart()))
        {
            blockEnd++;
        }

        paragraphs.RemoveRange(leadIn + 1, blockEnd - leadIn);

        return string.Join("\n\n", paragraphs);
    }
}
