using System.Text.RegularExpressions;

namespace Respondeo.SummaImporter;

// Prologue clean-up: trimming the enumerated article list that duplicates the on-page table of contents while preserving the lead-in sentence and every other prologue paragraph.
internal static partial class SummaParser
{
    // A standalone "PROLOGUE" heading line that introduces a part-level prologue (any indentation).
    [GeneratedRegex(@"^\s*PROLOGUE\s*$", RegexOptions.Compiled)]
    private static partial Regex PartPrologueHeadingRegex();

    // The Supplement has no "PROLOGUE"; instead it opens with an "EDITOR'S NOTE:" explaining that the
    // Supplement was compiled after Aquinas's death. We surface that note as the part's prologue.
    [GeneratedRegex(@"^\s*EDITOR'S NOTE:\s*$", RegexOptions.Compiled)]
    private static partial Regex PartEditorsNoteHeadingRegex();

    // Extracts a part-level prologue as a synthetic, article-less question in the part's first slot, or null when the part has no
    // explicit "PROLOGUE" (or Supplement "EDITOR'S NOTE:") heading before its first question. The prologue text is the paragraphs
    // following the heading up to the next rule divider, treatise heading, or question header, so treatise headings and the first
    // question stay out of it.
    //
    // Two parts have no prologue heading in the CCEL plain-text dump even though the printed Benziger 1947 edition opens them with
    // one: the First Part (the famous "Because the Master of Catholic Truth..." prologue to the whole Summa) and the Second Part of
    // the Second Part. Rather than edit the source dump, we fall back to a curated public-domain transcription for those parts (see
    // CuratedPartPrologues) so every part reads consistently while docs/summa.txt stays pristine.
    private static ParsedQuestion? ParsePartPrologue(string partId, string[] lines, int start, int end)
    {
        var headingLine = -1;
        var isEditorsNote = false;
        for (var i = start; i < end && i < lines.Length; i++)
        {
            if (PartPrologueHeadingRegex().IsMatch(lines[i]))
            {
                headingLine = i;
                break;
            }

            if (PartEditorsNoteHeadingRegex().IsMatch(lines[i]))
            {
                headingLine = i;
                isEditorsNote = true;
                break;
            }
        }

        if (headingLine < 0)
        {
            // No prologue heading in the source dump: use the curated public-domain text if we have one for this part.
            return CuratedPartPrologues.TryGetValue(partId, out var curated)
                ? BuildPrologue(partId, curated, "Prologue")
                : null;
        }

        // The body runs from just after the heading to the next structural boundary (rule line handled by ExtractText,
        // but treatise/question headers must stop it so nothing after the prologue leaks in).
        var bodyEnd = end;
        for (var i = headingLine + 1; i < end && i < lines.Length; i++)
        {
            if (TreatiseHeadingRegex().IsMatch(lines[i])
                || QuestionHeaderRegex().IsMatch(lines[i])
                || WrappedCountRegex().IsMatch(lines[i]))
            {
                bodyEnd = i;
                break;
            }
        }

        var text = ExtractText(lines, headingLine + 1, bodyEnd);
        if (string.IsNullOrWhiteSpace(text))
        {
            return CuratedPartPrologues.TryGetValue(partId, out var curated)
                ? BuildPrologue(partId, curated, "Prologue")
                : null;
        }

        // The Supplement's opening block is an editor's note rather than a prologue proper, so it carries its own label.
        return BuildPrologue(partId, text, isEditorsNote ? "Editor's Note" : "Prologue");
    }

    // Wraps prologue markdown into the synthetic first-slot pseudo-question shared by every part. The title doubles as the
    // browse-row label (e.g. "Prologue" or the Supplement's "Editor's Note").
    private static ParsedQuestion BuildPrologue(string partId, string markdown, string title)
    {
        var linkified = Linkify(markdown, partId, 0);
        return new ParsedQuestion($"{partId}-prologue", partId, 0, title, null, linkified, Array.Empty<ParsedArticle>())
        {
            IsPrologue = true,
        };
    }

    // Curated, public-domain part prologues for the parts whose CCEL plain-text dump omits the prologue heading that the printed
    // edition carries. Source: Summa Theologica, Benziger Bros. edition, 1947, translated by the Fathers of the English Dominican
    // Province (public domain). Cross-reference citations from the source (e.g. question and I-II article links) are omitted here.
    // Paragraphs are separated by a blank line and are emitted verbatim (no section cues, no inquiry list).
    private static readonly IReadOnlyDictionary<string, string> CuratedPartPrologues = new Dictionary<string, string>
    {
        // First Part (Prima Pars) - the prologue to the whole Summa.
        ["p1"] =
            "Because the Master of Catholic Truth ought not only to teach the proficient, but also to instruct beginners "
            + "(according to the Apostle: As unto little ones in Christ, I gave you milk to drink, not meat---1 Cor. 3:1, 2), we "
            + "purpose in this book to treat of whatever belongs to the Christian religion, in such a way as may tend to the "
            + "instruction of beginners. We have considered that students in this doctrine have not seldom been hampered by what "
            + "they have found written by other authors, partly on account of the multiplication of useless questions, articles, "
            + "and arguments; partly also because those things that are needful for them to know are not taught according to the "
            + "order of the subject-matter, but according as the plan of the book might require, or the occasion of the argument "
            + "offer; partly, too, because frequent repetition brought weariness and confusion to the minds of readers.\n\n"
            + "Endeavoring to avoid these and other like faults, we shall try, by God's help, to set forth whatever is included in "
            + "this sacred doctrine as briefly and clearly as the matter itself may allow.",

        // Second Part of the Second Part (Secunda Secundae).
        ["p2b"] =
            "After the general treatise of virtues and vices, and other things connected with the matter of morals, we must now "
            + "consider each of these things in particular. For there is less use in speaking about moral matters in general, since "
            + "actions are about particular things. Now moral matters can be considered in particular from two points of view. First, "
            + "from the point of view of the moral matter itself, for instance by considering a particular virtue or a particular vice. "
            + "Secondly, from the point of view of the special states of man, for instance by considering subjects and superiors, "
            + "active life and contemplative life, or any other differences among men. Accordingly, we shall treat first in a special "
            + "way of those matters which pertain to all the states of man; secondly, in a special way, of those matters which pertain "
            + "to particular states.\n\n"
            + "As to the first, we must observe that if we were to treat of each virtue, gift, vice, and precept separately, we should "
            + "have to say the same thing over and over again. For if one wished to treat adequately of this precept: Thou shalt not "
            + "commit adultery, one would have to inquire about adultery which is a sin, the knowledge of which depends on one's "
            + "knowledge of the opposite virtue. The shorter and quicker way, therefore, will be if we include the consideration of "
            + "each virtue, together with its corresponding gift, opposite vice, and affirmative and negative precepts, in the same "
            + "treatise. Moreover this way of treatment will be suitable to the vices according to their proper species. For it has "
            + "been shown above that vices and sins differ in species according to the matter or object, and not according to other "
            + "differences of sins, for instance, in respect of being sins of thought, word, and deed, or committed through weakness, "
            + "ignorance, or malice, and other like differences. Now the matter about which a virtue works what is right, and about "
            + "which the opposite vice deviates from the right, is the same.\n\n"
            + "Accordingly we may reduce the whole of moral matters to the consideration of the virtues, which themselves may be "
            + "reduced to seven in number. Three of these are theological, and of these we must treat first, while the other four are "
            + "the cardinal virtues, of which we shall treat afterwards. Of the intellectual virtues there is one, prudence, which is "
            + "included and numbered among the cardinal virtues. Art, however, does not pertain to moral science, which is concerned "
            + "with things to be done, for art is right reason about things to be made, as stated above. The other three intellectual "
            + "virtues, namely wisdom, understanding, and knowledge, agree, even in name, with some of the gifts of the Holy Ghost. "
            + "Therefore we shall consider them while considering the gifts corresponding to those virtues. The other moral virtues "
            + "are all in some way reducible to the cardinal virtues, as was explained above. Hence in treating about each cardinal "
            + "virtue we shall treat also of all the virtues which, in any way whatever, belong to that virtue, as also of the "
            + "opposite vices. In this way no matter pertaining to morals will be overlooked.",
    };

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
