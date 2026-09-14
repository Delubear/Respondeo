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
