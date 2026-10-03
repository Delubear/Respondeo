---
mode: agent
description: Add a new prayer to the Respondeo Discover corpus (Markdown + manifest).
---

# Add a prayer

Add a new prayer to `src/Respondeo.Content/wwwroot/discover/prayers/`.

If the prayer subject, title, or text was not provided, ask for it before proceeding. Only catalogue
public-domain or traditional texts.

Follow [`content-authoring.instructions.md`](../instructions/content-authoring.instructions.md) and
do all of the following:

1. Create `<slug>.md` with front-matter: `id` (= file stem), `title`, `summary`, `category`
   (`daily` | `marian` | `chaplet`), `language` (`en` by default), `translationKey`, `tags`, and
   `attribution` when a credit is needed.
2. Write the prayer body using the sense-line break rules in
   [`.github/copilot-instructions.md`](../copilot-instructions.md): break by petition/clause, blank
   line between sections, `Amen.` on its own line.
3. If a Latin companion is requested, create a second `language: la` file sharing the same
   `translationKey`, broken at the same sense lines.
4. Register every new file name in `prayers/prayers-manifest.json`.
5. Build and run the `Respondeo.UnitTests` Content tests; fix any failures.
