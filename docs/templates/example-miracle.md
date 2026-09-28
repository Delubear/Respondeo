---
# ────────────────────────────────────────────────────────────────────────────
# EXAMPLE MIRACLE FILE — copy this as a starting point for a new Discover miracle.
# Lives in src/Respondeo.Content.Discover/wwwroot/discover/miracles/.
# After copying: give it a unique `id` (matching the file name stem), edit the
# body, and add the file name to miracles/miracles-manifest.json so MiracleService
# will load it.
#
# The typed facet fields below (types, approval, region, …) power the browse
# page's facet filters and sort. Top-level `## ` headings split the body into
# titled prose sections.
# ────────────────────────────────────────────────────────────────────────────

id: example-miracle              # REQUIRED. Unique id / URL slug (/discover/miracles/example-miracle). Matches the file name stem.
title: "The Example Miracle"     # REQUIRED. Display title shown on cards and the detail page.
summary: A one-line description used on cards and previews.  # OPTIONAL.

types: [eucharistic]             # OPTIONAL. One or more type facets (e.g. eucharistic, marian, healing, incorruptible). Powers the "kind" filter.
approval: historical             # OPTIONAL. Judgment facet (e.g. approved, historical, studied). Powers the "judgment" filter.
region: europe                   # OPTIONAL. Region facet used by the region filter.
country: Italy                   # OPTIONAL. Country shown in the facts panel.
year: 750                        # OPTIONAL. Year used in the facts panel and for sort-by-year.
feastDay: "Corpus Christi"       # OPTIONAL. Associated feast day shown in the facts panel.

# OPTIONAL. Free-text tags used for search and shown on the detail page.
tags:
  - bleeding host
  - scientifically studied

# OPTIONAL. Reference list shown at the foot of the page. Each source has a
# required `label`; `url` is optional.
sources:
  - label: "Vatican International Exhibition of Eucharistic Miracles of the World"
    url: "http://www.miracolieucaristici.org/"
  - label: "Name of a study or primary source (no url)"
---

An untitled lead-in paragraph introducing the account.

## What happened

The narrative of the reported event. Use normal Markdown beneath each `## `
heading — paragraphs, lists, blockquotes, and links.

## How the Church regards it

Note the level of ecclesial approval and the Church's counsel of prudence:
such events are offered as aids to faith, not as articles of it.

## Why it matters

Close with the teaching the sign points to, not the basis for it.
