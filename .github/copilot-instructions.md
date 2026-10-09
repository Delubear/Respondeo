# Copilot Instructions

## General Guidelines
- Use American English spelling (e.g., "color", "behavior", "catalog", "center") in all user-facing text, content, and documentation for the Respondeo project, not British spelling.

## Line wrapping
- Keep lines under 200 characters, splitting them as necessary to stay within that limit.
- When a line must be split, prefer a break in one of these positions:
  - After a `.`
  - Before a `(`
  - After a `)`
  - After a `,`
  - After a `!`
  - After a `;`
- Avoid wrapping lines that already fit within the limit.
- Be context aware of the characters around a break point. For example, if a `.` is immediately followed by a `*` (such as Markdown emphasis like `*word*` or a bold/italic marker), do not break between them; keep the `*` with the `.` so the markup stays intact.

## Summa citations
- When writing a Summa Theologiae citation in content Markdown, match the visual style the app's `SummaReferenceRenderer` produces so hand-written citations read identically to the Summa's own cross-references.
- Use `Q.` (capital) for a question, `A.` (capital) for an article, `ad` for a reply, and `obj.` for an objection.
- Do not use lower-case `q.`/`a.` or the phrasing `reply to obj.`; write `ad N` for replies instead.
- Example: `[*Summa Theologiae* I, Q. 2, A. 3, ad 1](summa/prima-q002#article-3-reply-1)`.
- When a quotation is followed by its Summa citation, separate the quote from the citation with an em dash entity (`&mdash;`), never a plain hyphen (`-`). Example: `> "..."<br />&mdash; [*Summa Theologiae* I, Q. 2, A. 3](summa/prima-q002#article-3)`.

## Quotations vs. word emphasis (Inquiry content)
- Distinguish a *quotation* (a source's actual or imagined words) from a *mention* (a word or phrase discussed as a term). They must never share the same formatting.
- **Quotations** use plain double quotes with no italics: `"the gates of hell shall not prevail against it."` This applies to Scripture, Christ's words, the Summa, creeds, cited authors, and invented or hypothetical speech (e.g. an imagined objector's line). Do not wrap quotations in `*...*`.
- **Block quotations** stay as plain `> "..."` and are not italicized.
- **Mentions / scare-quotes** (a word referred to as a word, such as *holy*, *days*, *rock*, *novelties*, *four marks*) use italics only, with no quotation marks: write `*holy*`, not `*"holy"*` or `"holy"`.
- **Emphasis added inside a quotation** normally uses italics, not bold: `"by word of mouth *or* by letter."` Because quotations are no longer italic by default, italics are free to carry the emphasis.
- **Bold is allowed for deliberate rhetorical emphasis**, even on a quoted phrase, when the author is making a point or driving home a parallel (for example, the Scripture phrases that establish the Ark/Mary parallel in `church-objection-mary.md`). Use this sparingly and intentionally; do not convert such bold back to italics during a cleanup pass.
- **A bold leading statement or heading may contain italics** where italics fit convention — e.g. a bold lead sentence with an italic emphasis word or an italic word-mention (`***Hidden* is not *absent*.**`, `**The modern *spiritual but not religious*** view...`). Bold and italics may combine; italics still mark the mention or emphasis within the bolded span.
- Do not chain several double-quoted phrases together in one sentence; convert the ones that are mentions to italics so only true quotations keep their quotes.

## Prayer line breaks
- Prayer bodies (files under `discover/prayers/`) render through the line-break-preserving Markdown pipeline (`IContentHtmlRenderer.ToHtmlPreservingLineBreaks`), so a single newline becomes a `<br />` and a blank line starts a new paragraph. Author line breaks deliberately.
- Break prayers by *sense line*, the way missals and breviaries do: start a new line at each petition or clause, typically at semicolons, commas of direct address, and major phrase boundaries. Do not leave traditional prayers as a single run-on paragraph.
- Use a blank line to separate genuine sections or stanzas (for example, the two articles of the Apostles' Creed, or the salutation and petition of the Hail Mary). Use a single newline for lines within a section.
- Put `Amen.` on its own line, set off from the preceding text with a blank line.
- For a versicle and response, put each on its own line and set the pair off with a blank line. Prefix them with `V.` and `R.` (or `℣`/`℟`) consistently within a file.
- For Latin or other parallel translations (files sharing a `translationKey`), break at the same sense lines as the vernacular so the two texts line up.
- Do not use the trailing-two-space hard-break hack; rely on plain newlines since soft breaks are preserved.
- Short single-clause prayers (for example, the Sign of the Cross) may remain a single line.
- Reference texts for correct line breaks: the Roman Missal (Order of Mass prayers), the Liturgy of the Hours / Roman Breviary (antiphons and versicles), and a traditional hand missal or the Raccolta (devotional and Rosary prayers).

## Testing Guidelines
- Use the Respondeo.AcceptanceTests project for visual/E2E/acceptance tests rather than scaffolding a new one.

## Miracle Article Guidelines
- When writing Respondeo miracle articles (especially Eucharistic miracles), do not fault a reported miracle for lacking a "controlled trial" or "independent replication" — that is a category error, since a miracle is a singular act of God, not a repeatable experiment. Instead, distinguish what science can address (authenticity of the relic/sample, competence of the examination) from what it cannot (reproducing or witnessing the miraculous event itself).
