# Copilot Instructions

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
