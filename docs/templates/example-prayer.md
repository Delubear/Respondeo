---
# ────────────────────────────────────────────────────────────────────────────
# EXAMPLE PRAYER FILE — copy this as a starting point for a new Discover prayer.
# Lives in src/Respondeo.Content/wwwroot/discover/prayers/.
# After copying: give it a unique `id` (matching the file name stem), edit the
# body, and add the file name to prayers/prayers-manifest.json so PrayerService
# will load it.
#
# Copyright care: only catalogue prayers that are public-domain or traditional.
# Where a translation credit is required, add an `attribution` line.
# ────────────────────────────────────────────────────────────────────────────

id: example-prayer               # REQUIRED. Unique id / URL slug (/discover/prayers/example-prayer). Matches the file name stem.
title: "The Example Prayer"      # REQUIRED. Shown on cards and the prayer page.
sortKey: "Example Prayer"        # OPTIONAL. Filing key used when sorting by title. Set it when the title should sort differently from how it reads (e.g. a title leading with "The"/"A"/"An"). Defaults to title.
summary: A one-line description used on cards and previews.  # OPTIONAL.

category: marian                 # OPTIONAL. Category slug used for browse filtering (e.g. marian, daily, mass). Defaults to "other" (hidden from the filter).
language: en                     # OPTIONAL. Language of this text (en, la, …). Defaults to "en".
translationKey: example-prayer   # OPTIONAL. Shared key that pairs a prayer with its translation(s). Two files with the same key are the same prayer in different languages; the Latin (la) text is folded into the vernacular (en) page as LatinHtml.

# OPTIONAL. Free-text tags used for search AND the Tags browse filter.
tags:
  - basic
  - example

attribution: "Traditional English text (public domain)."  # OPTIONAL. Source / translation credit shown for copyright care.
---

This paragraph is the prayer text. Everything below the front matter is rendered
to HTML with normal Markdown. Keep it to the prayer itself.

To pair a Latin original with its English, create a second file with the same
`translationKey`, set its `language: la`, and register it in the manifest too —
the service shows the Latin alongside the vernacular on one page.
