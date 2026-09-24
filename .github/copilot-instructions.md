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
