# Authoring Discover Content

This project ships the **Discover** pillar — the content for *living* the Catholic faith — as static
web assets from the `Respondeo.Content.Discover` library. It holds four kinds of content:

| Kind | Format | Folder | Purpose |
|---|---|---|---|
| **Prayers** | Markdown (`.md`) | `wwwroot/discover/prayers/` | A single traditional/public-domain prayer. |
| **Devotions** | JSON (`.json`) | `wwwroot/discover/devotions/` | A data-driven prayer sequence (Rosary, chaplet, …). |
| **Articles** | Markdown (`.md`) | `wwwroot/discover/articles/` | A deeper dive on a practice or teaching. |
| **Miracles** | Markdown (`.md`) | `wwwroot/discover/miracles/` | A reported miracle with typed facet metadata. |

Every new file must be registered in its per-type manifest
([`wwwroot/discover/prayers/prayers-manifest.json`](wwwroot/discover/prayers/prayers-manifest.json),
[`wwwroot/discover/devotions/devotions-manifest.json`](wwwroot/discover/devotions/devotions-manifest.json),
[`wwwroot/discover/articles/articles-manifest.json`](wwwroot/discover/articles/articles-manifest.json),
[`wwwroot/discover/miracles/miracles-manifest.json`](wwwroot/discover/miracles/miracles-manifest.json)) to be loaded.

Ready-to-copy templates live under [`docs/templates/`](../../docs/templates):
[`example-prayer.md`](../../docs/templates/example-prayer.md),
[`example-devotion.json`](../../docs/templates/example-devotion.json),
[`example-article.md`](../../docs/templates/example-article.md), and
[`example-miracle.md`](../../docs/templates/example-miracle.md).

> **Copyright care:** only catalogue prayers that are public-domain or traditional. Where a
> translation credit is required, add an `attribution` line (see below).

---

## 1. Prayers (`wwwroot/discover/prayers/*.md`)

A prayer is a Markdown file: YAML front-matter between `---` lines, then the prayer text as body.

```markdown
---
id: hail-mary
title: "The Hail Mary"
summary: The angelic salutation joined to the Church's petition to the Mother of God.
category: marian
language: en
translationKey: hail-mary
tags:
  - basic
  - rosary
  - marian
attribution: "Traditional English text (public domain)."
---

Hail Mary, full of grace, the Lord is with thee; blessed art thou amongst women, and blessed is the
fruit of thy womb, Jesus. Holy Mary, Mother of God, pray for us sinners, now and at the hour of our
death. Amen.
```

### Front-matter fields

| Field | Required | Description |
|---|---|---|
| `id` | Yes | Stable id / URL slug (lowercase, hyphens). Must match the file name stem. |
| `title` | Yes | Display title shown on cards and the prayer page. |
| `summary` | No | One-line description used on cards and previews. |
| `category` | No | Category slug used for browse filtering (e.g. `marian`, `daily`, `mass`). Defaults to `other` (hidden from the filter). |
| `language` | No | Language of this text (`en`, `la`, …). Defaults to `en`. |
| `translationKey` | No | Shared key that pairs a prayer with its translation(s). Two files with the same key are the same prayer in different languages; the service shows the Latin alongside the vernacular. |
| `tags` | No | Free-text tags used for search **and** the Tags browse filter. |
| `attribution` | No | Source / translation credit shown for copyright care. |

**Pairing a Latin original with its English:** give both files the same `translationKey` and set each
file's `language`. The English (`en`) file is the primary entry; the Latin (`la`) text is folded into
it as `LatinHtml`.

---

## 2. Devotions (`wwwroot/discover/devotions/*.json`)

A devotion is a JSON document describing an ordered **sequence** of steps that reference prayers by
`id`, plus optional selectable **mystery sets**. The interactive player walks the sequence, so a new
devotion is added purely as data — no code change.

```json
{
  "id": "divine-mercy-chaplet",
  "title": "The Chaplet of Divine Mercy",
  "summary": "A short chaplet prayed on ordinary Rosary beads, pleading God's mercy for the world.",
  "kind": "chaplet",
  "intro": "Given to St. Faustina Kowalska, the Chaplet of Divine Mercy is prayed on Rosary beads…",
  "mysterySets": [
    {
      "id": "decades",
      "name": "The Five Decades",
      "when": "Optional note on when it is prayed",
      "summary": "Optional short description shown on the set's selection card.",
      "mysteries": [
        { "title": "First Decade", "reflection": "Optional *Markdown* meditation." }
      ]
    }
  ],
  "sequence": [
    { "kind": "prayer", "title": "Begin", "prayerId": "sign-of-the-cross", "bead": "cross" },
    { "kind": "prayer", "prayerId": "our-father", "bead": "large" },
    { "kind": "prayer", "prayerId": "apostles-creed", "bead": "between" },
    {
      "kind": "mysteries",
      "title": "The Decades",
      "perMystery": [
        { "kind": "prayer", "title": "On the Our Father bead", "prayerId": "divine-mercy-eternal-father", "bead": "large" },
        { "kind": "prayer", "title": "On the Hail Mary beads", "prayerId": "divine-mercy-for-the-sake", "repeat": 10, "bead": "small" }
      ]
    },
    { "kind": "prayer", "prayerId": "divine-mercy-holy-god", "repeat": 3, "bead": "small" },
    { "kind": "prayer", "prayerId": "sign-of-the-cross", "bead": "cross" }
  ]
}
```

### Top-level fields

| Field | Required | Description |
|---|---|---|
| `id` | Yes | Stable id / URL slug. Must match the file name stem. |
| `title` | Yes | Display title. |
| `summary` | No | One-line description for cards. |
| `kind` | No | Kind slug used for grouping, iconography, and the Kind browse filter (e.g. `rosary`, `chaplet`, `litany`). Defaults to `devotion` (hidden from the filter). |
| `intro` | No | Markdown introduction shown before the reader begins. |
| `mysterySets` | No | Selectable meditation sets (see below). Empty for devotions with no per-mystery meditation. |
| `sequence` | Yes | The ordered steps of the devotion. |

### Mystery sets

| Field | Required | Description |
|---|---|---|
| `id` | Yes | Slug for the set (e.g. `joyful`). |
| `name` | Yes | Display name (e.g. `The Joyful Mysteries`). |
| `when` | No | Note on when the set is prayed (e.g. `Mondays and Saturdays`). |
| `summary` | No | Short description shown on the set's selection card. |
| `mysteries` | Yes | Ordered meditations; each has a `title` and an optional Markdown `reflection`. |

### Sequence steps

| Field | Required | Description |
|---|---|---|
| `kind` | No | `prayer` (default) prays a single prayer; `mysteries` iterates the chosen mystery set. |
| `title` | No | Optional heading for the step (e.g. `Begin`, `Closing prayers`). |
| `prayerId` | For `prayer` steps | The `id` of a prayer in `wwwroot/discover/prayers/`. |
| `repeat` | No | How many times to pray the step (e.g. `10` for a decade). Defaults to `1`. Each repetition is its own row/bead. |
| `bead` | No | Marker drawn on the left-hand rosary "thread" beside the step. One of `cross` (Sign of the Cross), `medal` (centerpiece medal), `large` (Our Father bead), `small` (Hail Mary bead), or `between` (prayed between beads, drawn as three small pips). Omit for spoken steps that are not prayed on a bead. |
| `perMystery` | For `mysteries` steps | The step template run once per mystery of the chosen set (a list of `prayer` steps). |

Every `prayerId` must refer to an existing prayer file, or the player has nothing to render.

---

## 3. Articles (`wwwroot/discover/articles/*.md`)

An article is Markdown with front-matter. Top-level `## ` headings split the body into titled
sections; the text before the first heading is the untitled lead-in.

```markdown
---
id: confession
title: "Confession: Why Catholics Confess to a Priest"
summary: Where the Sacrament of Reconciliation comes from, and how to make a good confession.
topic: sacraments
tags:
  - confession
  - reconciliation
sources:
  - label: "Catechism of the Catholic Church, 1420–1498"
    url: "https://www.vatican.va/archive/ENG0015/__P4C.HTM"
  - label: "Council of Trent, Session 14 (1551)"
---

An untitled lead-in paragraph.

## Where it comes from

The first titled section…
```

### Front-matter fields

| Field | Required | Description |
|---|---|---|
| `id` | Yes | Stable id / URL slug. Must match the file name stem. |
| `title` | Yes | Display title. |
| `summary` | No | One-line description shown on cards and (italicised) atop the article. |
| `topic` | No | Topic slug used for the Topic browse filter (e.g. `sacraments`, `practice`, `apologetics`). Defaults to `general` (hidden from the filter). |
| `tags` | No | Free-text tags used for search. |
| `sources` | No | Citations / further reading; each entry has a `label` and an optional `url`. |

Sections are formed by `## ` headings in the body; write normal Markdown beneath each.

---

## 4. Adding new content — checklist

1. Create the file in the matching folder (`prayers/`, `devotions/`, or `articles/`) with `id`
   matching the file name stem.
2. Register the file name in that content type's manifest
   (`prayers/prayers-manifest.json`, `devotions/devotions-manifest.json`, or
   `articles/articles-manifest.json`).
3. For devotions, confirm every `prayerId` refers to an existing prayer file.
4. For paired translations, give both files the same `translationKey` and set each `language`.
5. Run the app and verify the item lists, filters, and detail page render correctly.
