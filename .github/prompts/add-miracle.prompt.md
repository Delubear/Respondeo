---
mode: agent
description: Add a new miracle to the Respondeo Discover corpus (Markdown + facets + manifest).
---

# Add a miracle

Add a new reported miracle to `src/Respondeo.Content/wwwroot/discover/miracles/`.

If the miracle, location, year, or type was not provided, ask before proceeding.

Follow [`content-authoring.instructions.md`](../instructions/content-authoring.instructions.md) and
do all of the following:

1. Create `<slug>.md` with front-matter: `id` (= file stem), `title`, `sortKey` (drop a leading
   "The"), `summary`, `types` (list), `approval`, `region`, `country`, `year`, optional `feastDay`,
   `tags`, and `sources` (`{ label, url? }`).
2. Use only facet slugs that exist in `miracles/facets.json`:
   - `types`: `eucharistic`, `marian`, `healing`, `incorruptible`, `stigmata`, `image`, `other`
   - `approval`: `approved`, `under-investigation`, `not-approved`, `historical`
   - `region`: `europe`, `north-america`, `latin-america`, `africa`, `asia`, `middle-east`, `oceania`, `unknown`
3. Write the body as `## `-sectioned Markdown (e.g. What happened / How the Church regards it / Why
   it matters). Present approval status honestly; set `reviewStatus: unvetted` for unverified accounts.
4. Register the file name in `miracles/miracles-manifest.json`.
5. Build and run the `Respondeo.UnitTests` Content tests; fix any failures.
