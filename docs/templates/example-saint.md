---
# ────────────────────────────────────────────────────────────────────────────
# EXAMPLE SAINT FILE — copy this as a starting point for a new Discover saint.
# Lives in src/Respondeo.Content/wwwroot/discover/saints/.
# After copying: give it a unique `id` (matching the file name stem), edit the
# body, and add the file name to saints/saints-manifest.json so SaintService
# will load it.
#
# The typed facet fields below (era, region, patronages, …) power the browse
# page's facet filters and sort. Top-level `## ` headings split the body into
# titled prose sections.
# ────────────────────────────────────────────────────────────────────────────

id: example-saint               # REQUIRED. Unique id / URL slug (/discover/saints/example-saint). Matches the file name stem.
title: "St. Example of Place"   # REQUIRED. Display title shown on cards and the detail page.
sortKey: "Example of Place"     # OPTIONAL. Filing key used when sorting by name. Set it when the title should sort differently from how it reads (e.g. drop a leading "St."/"The"). Defaults to title.
summary: A one-line description used on cards and previews.  # OPTIONAL.

era: modern                     # OPTIONAL (single). Historical era facet: early-church, medieval, early-modern, modern.
region: europe                  # OPTIONAL (single). Region facet used by the region filter.
patronages: ["Missions", "Youth"]   # OPTIONAL (free-text list). Patronages — searched and shown, but NOT a filter facet, so any wording is allowed. Reuse existing wording/casing for consistency.
statesOfLife: [religious]       # REQUIRED (list, >=1). States of life: religious, deacon, priest, bishop, pope, lay, martyr, virgin, widow. Holy orders are cumulative (deacon -> priest -> bishop -> pope); list every state that applies.
canonizations: [canonized]      # REQUIRED (list, >=1). Canonization status(es): canonized, beatified, venerable, servant-of-god, pre-congregation, doctor-of-the-church.
dates: "1873-1897"              # OPTIONAL. Free-text life dates shown in the facts panel. Use a plain "-" for ranges; the pipeline renders it as an en dash.
feastDay: "October 1"           # OPTIONAL. Feast day shown in the facts panel.

# OPTIONAL. Free-text tags used for search and shown on the detail page.
tags:
  - example
  - little way

# OPTIONAL. Reference list shown at the foot of the page. Each source has a
# required `label`; `url` is optional.
sources:
  - label: "Autobiography or primary source (no url)"
  - label: "Vatican biography"
    url: "https://www.vatican.va/"

# OPTIONAL. Flags AI-drafted or not-yet-reviewed content so the page renders a
# standard notice. Remove once the profile has been reviewed.
reviewStatus: unvetted
---

An untitled lead-in paragraph introducing the saint.

## His/Her life

The narrative of the saint's life. Use normal Markdown beneath each `## `
heading — paragraphs, lists, blockquotes, and links.

## Why it matters

What this saint's example offers today.
