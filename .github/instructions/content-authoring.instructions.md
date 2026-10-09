---
description: Schemas and rules for authoring Respondeo Discover content (prayers, devotions, articles, miracles, saints)
applyTo: "src/Respondeo.Content/wwwroot/discover/**"
---

# Discover Content Authoring Rules

When creating or editing a prayer, devotion, article, miracle, or saint under
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
   - saints -> `saints/saints-manifest.json`
3. **Copyright care:** only catalog prayers/texts that are public-domain or traditional. When a
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

**Review status (articles, miracles, and inquiry nodes only):** these types may carry
`reviewStatus: unvetted` in their front-matter to flag AI-drafted or not-yet-reviewed content; the
page renders a standard notice. Do **not** hand-write an inline HTML note in the body. Prayers and
devotions are short, fixed traditional texts and do not use this flag.

## Quotations and emphasis (articles, miracles, saints, inquiry nodes)

For any set-off Markdown block quote (a `>` block) in prose content, use one consistent style:

- **No surrounding quotation marks.** A `>` block is already visually set off, so wrapping the quoted
  text in `"…"` is redundant. Reserve `"…"` for short *inline* quotations run into your own sentence.
  Internal quotation marks that belong to the quoted text (e.g. a word in single quotes) stay.
- **Attribution on its own line**, as the last line of the same blockquote, after a blank `>`
  separator, prefixed with the `&mdash;` entity:

  ```markdown
  > Quoted sentence that stands on its own as a block.
  >
  > &mdash; Source, *Work*
  ```

- For a Summa quotation the attribution line *is* the canonical Summa citation link, e.g.
  `> &mdash; [*Summa Theologiae* I, Q. 2, A. 3](summa/prima-q002#article-3)` (the citation text still
  follows the `Q.`/`A.` style enforced by `ContentLinkIntegrityTests`).
- **Do not** join the quote and attribution with `<br />` or append `&mdash; Source` to the end of the
  quoted sentence.
- **Emphasis uses `*italics*`, never quotation marks.** Italicize an emphasized word or phrase
  (`the *public* claim`); do not use scare-quotes for emphasis.

## Punctuation characters (all content Markdown)

The only literal dash character allowed in content Markdown is the plain ASCII hyphen `-`. Every other
dash or special typographic mark must be written as an HTML entity (or plain ASCII), never as the raw
Unicode glyph. This keeps the source unambiguous in a monospace editor, diff-able, and grep-able.

- **Hyphen** `-` (ASCII): the only literal dash. Use for compound words (`self-evident`), list bullets,
  and YAML. Never paste a Unicode em/en dash as a bare `-` replacement.
- **Em dash** &rarr; `&mdash;` (renders as the long dash). Use for breaks in thought within prose and for
  block-quote attribution lines. Do **not** type a literal `—`.
- **En dash** &rarr; `&ndash;` (renders as the medium dash). Use for numeric ranges only, e.g.
  `Romans 6:3&ndash;4`, `CCC 1213&ndash;1284`. Do **not** type a literal `–`.
- **Ellipsis** &rarr; three ASCII dots `...` (not the single `…` glyph and not `&hellip;`). Use for an
  omission inside a quotation.
- **Quotation marks** are straight ASCII `"` and `'`; do not paste curly/smart quotes.

Rationale: the content renderer (`MarkdigContentHtmlRenderer`) does **not** enable SmartyPants, so no
dash or quote auto-conversion happens &mdash; whatever glyph is authored is exactly what ships. Writing
entities makes the intended mark explicit and reviewable.

### Don't overuse the em dash

The em dash is for a *genuine* pause: a true parenthetical aside (one you could lift out and still have
a whole sentence) or a deliberate dramatic break. It is **not** a general-purpose "insert a pause here"
mark. Because it can stand in for a comma, colon, semicolon, or period all at once, it is easy to
overuse until every sentence sounds the same and none of the dashes land.

Before writing `&mdash;` in prose, check whether another mark does the job better:

- Introduces or explains what follows &rarr; use a **colon** (`:`).
- Joins two complete, independent thoughts &rarr; use a **period** (`.`) or **semicolon** (`;`).
- Wraps a mild aside, appositive, or nonrestrictive clause &rarr; use **commas** (`,`).
- Marks a real break in thought or a genuine parenthetical set-off from the sentence &rarr; keep the
  **em dash**.

Soft limit: aim for **at most one prose em dash per paragraph**. Block-quote attribution lines
(`> &mdash; Source`) are a fixed convention and are exempt from this limit.

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

## Saints (`saints/*.md`)

Markdown with front-matter carrying typed **facet** metadata, then `## `-sectioned body.

- Extra fields: `era`, `region`, `patronages` (free-text list), `statesOfLife` (list),
  `designations` (list), `sex` (single), `religiousOrders` (list), `canonizations` (list),
  `dates`, `feastDay`, `sources`.
- **Facet slugs must exist in `saints/facets.json`** — enforced by test. Allowed values:
  - `era` (single): `early-church`, `medieval`, `early-modern`, `modern`
  - `region` (single): `europe`, `north-america`, `latin-america`, `africa`, `asia`, `middle-east`, `oceania`, `unknown`
  - `statesOfLife` (list): `religious`, `deacon`, `priest`, `bishop`, `pope`, `lay`
  - `designations` (list): `apostle`, `evangelist`, `prophet`, `martyr`, `virgin`, `widow`, `founder`, `doctor-of-the-church`
  - `sex` (single): `male`, `female`
  - `religiousOrders` (list): `augustinian`, `dominican`, `franciscan`, `capuchin`, `carmelite`
  - `canonizations` (list): `canonized`, `beatified`, `venerable`, `servant-of-god`, `pre-congregation`
- **`statesOfLife` and `canonizations` are multi-valued and required** (at least one each) — enforced
  by test. A saint may hold several (e.g. `[priest, bishop]`, `[canonized]`).
- **`designations`, `sex`, and `religiousOrders` are optional.** Add each that genuinely applies.
- **List every state of life that genuinely applies.** The slugs are independent descriptors, not a
  ranked ladder, and the browse filter matches any saint whose list *contains* a selected state, so
  being thorough only makes a saint easier to find (never excludes them).
  - **Holy orders are cumulative in reality, so include each order the saint holds** — the ladder is
    `deacon` → `priest` → `bishop` → `pope`. A priest is also a deacon; a bishop is also a priest
    and deacon; a pope is also a bishop, priest, and deacon. Use `[deacon, priest, bishop]` for a
    bishop and `[deacon, priest, bishop, pope]` for a pope, so each surfaces under every order's
    filter. A saint ordained only to the diaconate (e.g. St. Francis of Assisi) is just `[deacon]`.
  - **`religious`** means a vowed member of a religious order (Franciscan, Carmelite, etc.), as
    opposed to diocesan/secular clergy. Add it whenever it applies, and omit it for secular clergy.
    So a friar who is also a bishop is `[priest, bishop, religious]`; a diocesan bishop is
    `[priest, bishop]`; a non-ordained friar is `[religious]`.
  - **`designations`** carry honorific/role descriptors that are not states of life: `apostle`,
    `evangelist`, `prophet`, `martyr`, `virgin`, `widow`, `founder` (of a religious order, movement,
    or institute), and `doctor-of-the-church`. They are independent — add each that applies (e.g.
    `[martyr]`, `[virgin, doctor-of-the-church]`, `[founder]`).
  - **`religiousOrders`** names the institute(s) a `religious` saint belongs to or founded (e.g.
    `[dominican]`). Omit it for secular clergy and for founders of non-order institutes (e.g. Opus
    Dei, a personal prelature). Only tag an order the saint actually belonged to — do not tag a saint
    with an order merely named after them if it was founded after their lifetime.
  - **`sex`** is a single value, `male` or `female`.
- `patronages` is **free-text display values** (e.g. `["Missions", "The poor"]`), not facet slugs — it
  is searched and shown on cards/detail but is **not** a filter facet, so it is not listed in
  `facets.json`. Reuse existing wording/casing for consistency.
- Prefer `sources` with reputable references. Set `reviewStatus: unvetted` for unverified profiles.
