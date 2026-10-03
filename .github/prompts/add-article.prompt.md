---
mode: agent
description: Add a new article to the Respondeo Discover corpus (Markdown + manifest).
---

# Add an article

Add a new article to `src/Respondeo.Content/wwwroot/discover/articles/`.

If the article subject or angle was not provided, ask before proceeding.

Follow [`content-authoring.instructions.md`](../instructions/content-authoring.instructions.md) and
do all of the following:

1. Create `<slug>.md` with front-matter: `id` (= file stem), `title`, `summary`, `topic` (reuse an
   existing slug such as `sacraments` where possible), `tags`, and `sources` (`{ label, url? }`).
2. Write the body in Markdown: an untitled lead-in paragraph, then `## `-headed sections. Cite the
   Catechism / councils / Scripture where relevant; respect the Summa citation style from
   [`.github/copilot-instructions.md`](../copilot-instructions.md).
3. Set `reviewStatus: unvetted` if the draft has not been theologically reviewed.
4. Register the file name in `articles/articles-manifest.json`.
5. Build and run the `Respondeo.UnitTests` Content tests; fix any failures.
