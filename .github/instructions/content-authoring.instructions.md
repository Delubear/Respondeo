---
description: Schemas and rules for authoring Respondeo Discover content (prayers, devotions, articles, miracles)
applyTo: "src/Respondeo.Content/wwwroot/discover/**"
---

# Discover Content Authoring Rules

When creating or editing a prayer, devotion, article, or miracle under
`src/Respondeo.Content/wwwroot/discover/`, follow these rules exactly. The authoritative long-form
guide is [`DISCOVER.md`](../../src/Respondeo.Content/DISCOVER.md); this file is the quick, enforceable
contract. Ready-to-copy templates live under `docs/templates/`.

## Golden rules (every type)

1. **File name stem must equal the `id`.** `hail-mary.md` has `id: hail-mary`.
2. **Register the new file in its manifest** — this is the most-forgotten step and is enforced by a
   unit test:
   - prayers -> `prayers/prayers-manifest.json`
   - devotions -> `devotions/devotions-manifest.json`
   - articles -> `articles/articles-manifest.json`
   - miracles -> `miracles/miracles-manifest.json`
3. **Copyright care:** only catalogue prayers/texts that are public-domain or traditional. When a
   translation credit is required, add an `attribution` line.
4. **`id` / slug:** lowercase, hyphenated, stable.
5. After authoring, build and run the content unit tests (`Respondeo.UnitTests` → `Content` folder).

## Shared front-matter fields (prayers, articles, miracles)

| Field | Required | Notes |
|---|---|---|
| `id` | Yes | Lowercase slug; must match file stem. |
| `title` | Yes | Display title. |
| `summary` | No | One-line card description. |
| `sortKey` | No | Filing key when the title should sort differently (e.g. drop a leading "The"). |
| `tags` | No | Free-text tags for search/filter. |

**Review status (articles and miracles only):** these two types may carry `reviewStatus: unvetted`
in their front-matter to flag AI-drafted or not-yet-reviewed content; the detail page renders a
standard notice. Do **not** hand-write an inline HTML note in the body. Prayers and devotions are
short, fixed traditional texts and do not use this flag.

## Prayers (`prayers/*.md`)

Markdown: YAML front-matter, then the prayer text as the body.

- Extra fields: `category`, `language` (default `en`), `translationKey`, `attribution`.
- `category` allowed slugs: `daily`, `marian`, `chaplet`. (Unknown values default to `other` and are
  hidden from the filter — prefer an existing slug.)
- **Line breaks matter:** follow the prayer sense-line rules in `.github/copilot-instructions.md`
  (break by petition/clause, blank line between sections, `Amen.` on its own line).
- **Latin pairing:** give the Latin (`language: la`) file and its English (`language: en`) file the
  same `translationKey`; break both at the same sense lines. The English file is primary.

## Devotions (`devotions/*.json`)

JSON only — a data-driven prayer sequence; no code change needed.

- Top-level: `id`, `title` (required), `sortKey`, `summary`, `kind`, `intro`, `mysterySets`,
  `sequence` (required).
- `kind` example slugs: `rosary`, `chaplet`, `litany` (defaults to `devotion`).
- **Every `prayerId` in `sequence`/`perMystery` must refer to an existing prayer file** — enforced
  by test. Add any missing prayer first.
- Sequence step `bead` values: `cross`, `medal`, `large`, `small`, `between` (omit for non-bead steps).

## Articles (`articles/*.md`)

Markdown with front-matter; `## ` headings split the body into sections, text before the first
heading is the lead-in.

- Extra fields: `topic`, `sources` (list of `{ label, url? }`).
- `topic` currently used: `sacraments`. Reuse an existing topic slug where possible.

## Miracles (`miracles/*.md`)

Markdown with front-matter carrying typed **facet** metadata, then `## `-sectioned body.

- Extra fields: `types` (list), `approval`, `region`, `country`, `year`, `feastDay`, `sources`.
- **Facet slugs must exist in `miracles/facets.json`** — enforced by test. Allowed values:
  - `types`: `eucharistic`, `marian`, `healing`, `incorruptible`, `stigmata`, `image`, `other`
  - `approval`: `approved`, `under-investigation`, `not-approved`, `historical`
  - `region`: `europe`, `north-america`, `latin-america`, `africa`, `asia`, `middle-east`, `oceania`, `unknown`
- Prefer `sources` with reputable references. Set `reviewStatus: unvetted` for unverified accounts.
