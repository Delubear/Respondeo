# Authoring Miracle Entries

This project ships the **Miracles** catalog as static web assets from the
`Respondeo.Content.Miracles` library. Each entry is a single Markdown file made of two parts:

1. **YAML front-matter** — structured metadata between two `---` lines, used for the browse index,
   the facet filters (kind, the Church's judgment, region), and search.
2. **Markdown body** — the readable account, split into titled sections by `## ` headings.

Every new file must be registered in
[`wwwroot/miracles/miracles-manifest.json`](wwwroot/miracles/miracles-manifest.json) to be loaded.

> **Unvetted content:** entries are a work in progress. Keep the standard note at the top of the body
> until the account has been reviewed.

---

## 1. File structure

```markdown
---
id: lanciano-eucharistic-miracle
title: "The Eucharistic Miracle of Lanciano"
summary: An 8th-century host reported to have become human flesh and blood, examined scientifically in 1971.
types: [eucharistic]
approval: historical
region: europe
country: Italy
year: 750
feastDay: "Corpus Christi"
tags:
  - bleeding host
  - flesh and blood
  - scientifically studied
sources:
  - label: "Vatican International Exhibition of Eucharistic Miracles of the World"
    url: "http://www.miracolieucaristici.org/"
  - label: "Odoardo Linoli, study of the relics (1971)"
---

<b>NOTE: This is unvetted content. It is a work in progress and may contain errors.</b>

The account's lead-in paragraph…

## What happened

The first titled section…
```

### Front-matter fields

| Field | Required | Description |
|---|---|---|
| `id` | Yes | Stable id / URL slug (lowercase, hyphens). Must match the file name stem. |
| `title` | Yes | Display title shown on cards and the detail page. |
| `summary` | No | One-line description used on cards and previews. |
| `types` | No | One or more **category** slugs (e.g. `[eucharistic, healing]`). Drives the *Kind* facet. |
| `approval` | No | The Church's judgment slug (e.g. `approved`, `under-investigation`, `not-approved`, `historical`). Drives the *Church's judgment* facet. |
| `region` | No | Region slug (e.g. `europe`, `latin-america`). Drives the *Region* facet. |
| `country` | No | Human-readable country name, shown on cards and searchable. |
| `year` | No | Year of the event (integer). Used for the century label and year sorting. Negative for BC. |
| `feastDay` | No | Associated feast, if any. |
| `tags` | No | Free-text tags used for search. |
| `sources` | No | Citations / further reading; each entry has a `label` and an optional `url`. |

The `types`, `approval`, and `region` slugs must exist in
[`wwwroot/miracles/facets.json`](wwwroot/miracles/facets.json), which maps each slug to its display
label. **Add the slug there first** if you introduce a new value, otherwise the facet has no label.
`MiracleFacetIntegrityTests` checks that every slug used by an entry is defined in `facets.json`.

---

## 2. Writing the body

The body is standard Markdown. Top-level `## ` headings split it into titled sections; text before
the first heading is the untitled lead-in. Keep the unvetted-content note as the first line until the
account is reviewed.

---

## 3. Adding a new miracle — checklist

1. Create `wwwroot/miracles/<id>.md` with front-matter and body (`id` matching the file name stem).
2. If you use a new `types`, `approval`, or `region` slug, add it to
   [`facets.json`](wwwroot/miracles/facets.json) with a display label.
3. Register the file name in
   [`miracles-manifest.json`](wwwroot/miracles/miracles-manifest.json).
4. Run the app and verify the entry appears, its facets filter correctly, and the detail page renders.
