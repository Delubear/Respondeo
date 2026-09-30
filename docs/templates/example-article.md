---
# ────────────────────────────────────────────────────────────────────────────
# EXAMPLE ARTICLE FILE — copy this as a starting point for a new Discover article.
# Lives in src/Respondeo.Content/wwwroot/discover/articles/.
# After copying: give it a unique `id` (matching the file name stem), edit the
# body, and add the file name to articles/articles-manifest.json so ArticleService
# will load it.
#
# Top-level `## ` headings split the body into titled sections; the text before
# the first heading is the untitled lead-in.
# ────────────────────────────────────────────────────────────────────────────

id: example-article              # REQUIRED. Unique id / URL slug (/discover/articles/example-article). Matches the file name stem.
title: "An Example Article"      # REQUIRED. Display title shown on cards and the article page.
sortKey: "Example Article"       # OPTIONAL. Filing key used when sorting by title. Set it when the title should sort differently from how it reads (e.g. a title leading with "The"/"A"/"An"). Defaults to title.
summary: One-line description shown on cards and (italicised) atop the article.  # OPTIONAL.

topic: sacraments                # OPTIONAL. Topic slug used for the Topic browse filter (e.g. sacraments, prayer, the-mass).

# OPTIONAL. Free-text tags used for search AND the Tags browse filter.
tags:
  - example
  - the catholic church

# OPTIONAL. Reference list shown at the foot of the article. Each source has a
# required `label`; `url` is optional (a bare label renders as plain text).
sources:
  - label: "Catechism of the Catholic Church, 1420–1498"
    url: "https://www.vatican.va/archive/ENG0015/__P4C.HTM"
  - label: "Council of Trent, Session 14 (1551)"
---

This untitled lead-in paragraph appears before the first section heading.

## Where it comes from

The first titled section. Use normal Markdown beneath each `## ` heading —
paragraphs, lists, blockquotes, links, and the same media directives available
in any content file, e.g. an embedded video:

::: youtube Euxu36jVJpU
:::

## Why it matters

Another section. Internal links navigate without a full page reload:

[Back to the overview of all seven sacraments](discover/articles/the-sacraments)
