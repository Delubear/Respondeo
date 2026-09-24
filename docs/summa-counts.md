# Summa Theologica — Question & Article Counts

This document records the canonical structure of the Summa Theologica as reproduced by our corpus, and
explains how our generated counts relate to the **Benziger Bros. 1947 edition** (the Fathers of the English
Dominican Province translation) that `docs/summa.txt` is derived from.

The counts here are pinned by the automated test
[`Per_part_question_and_article_counts_match_the_Benziger_1947_edition`](../tests/Respondeo.UnitTests/Content/SummaCorpusShapeTests.cs).
If the importer ever drifts, that test fails instead of silently corrupting the corpus.

## Canonical per-part counts

| Part | Latin name | Questions | Articles |
|------|------------|----------:|---------:|
| First Part (I) | Prima Pars | 119 | 584 |
| First Part of the Second Part (I–II) | Prima Secundae | 114 | 619 |
| Second Part of the Second Part (II–II) | Secunda Secundae | 189 | 917 |
| Third Part (III) | Tertia Pars | 90 | 549 |
| Supplement | Supplementum | 102 | 456 |
| **Total** | | **614** | **3,125** |

The four main parts (119 / 114 / 189 / 90 = 512 questions) match the universally-cited canonical question
counts exactly.

## How this relates to the commonly-quoted "512 questions / 2,669 articles" or "612 / 3,120" figures

Several older summaries (e.g. the Catholic Encyclopedia) quote round figures such as *"612 questions,
subdivided into 3,120 articles."* Those numbers predate careful digital tallies and differ from ours for two
well-understood, deliberate reasons — **not** because of any parsing error:

1. **The Supplement's appendices are surfaced as questions.**
   The Supplement proper is **99 questions**. The Benziger/CCEL text then appends three sections under two
   "Appendix" headings:
   - Appendix 1 — *"Of the Quality of Those Souls Who Depart This Life with Original Sin Only"* (`sup-q100`, 2 articles)
   - Appendix 1 (cont.) — *"Of the Quality of Souls Who Expiate Actual Sin or Its Punishment in Purgatory"* (`sup-q101`, 6 articles)
   - Appendix 2 — *"Two Articles on Purgatory"* → titled **On Purgatory** (`sup-q102`, 2 articles)

   Traditional numbering stops at Q99 and treats these as unnumbered appendices. We expose them as questions
   100–102 so their content is browsable. This accounts for the 3 extra Supplement questions.

2. **Article totals are reconciled against the source's own declared markers.**
   Every question header in `docs/summa.txt` declares its own count as `(N ARTICLES)`, and every question's
   inquiry list enumerates the same articles. Our importer's output matches those declared markers
   **question-by-question** (614 headers declaring 3,125 articles → 614 generated questions / 3,125 articles).
   The famous round figures do not consistently include the appendix articles or a handful of articles that
   run together in the source with no divider.

## Known source-text errata (tolerated, not fixed)

`docs/summa.txt` contains a small number of genuine typos from the printed edition that the importer
faithfully reproduces rather than silently "correcting":

- Duplicate section numbers within an article (e.g. two paragraphs both labelled `Objection 2` where the
  second should be `Objection 3`).
- One period-delimited objection header (`Objection 1.` instead of `Objection 1:`), which the importer now
  tokenises correctly.

These are edition-level errata, not count discrepancies, and are deliberately preserved. The corpus-shape
tests distinguish them from real parser bugs (which show up as section numbering *restarting* mid-article).

## Importer boundary handling

Three classes of source irregularity are handled by the importer so that the counts above hold:

- **Titles with trailing footnotes** — e.g. `"... pleasant? [*"Bonum honestum" ...]"`; the title is cut at
  the first `?` and the annotation is skipped.
- **Declarative article titles** — a few articles use a statement title (e.g. *"The difference of aeviternity
  and time"*) with no `Whether ...?`; accepted when directly followed by `Objection 1`.
- **Undivided articles** — a few articles run together in the source with no rule divider and no second
  title; the boundary is detected (a fresh `Objection 1` after a `Reply to Objection`) and the missing title
  is recovered from the question's inquiry list.
- **Count-first appendix headings** — e.g. `TWO ARTICLES ON PURGATORY`, recognised as a question header so
  the appendix content is not folded into the preceding question.
