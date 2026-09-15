---
# ─────────────────────────────────────────────────────────────────────────────
# EXAMPLE CONTENT FILE — copy this as a starting point for a new node.
# Everything between the --- fences is YAML front matter (structured metadata).
# After copying: give it a unique `id`, edit the body, and add the file name to
# manifest.json so ContentService will load it.
# ─────────────────────────────────────────────────────────────────────────────

id: example                      # REQUIRED. Unique id and URL slug (/node/example).
title: "Example Node"            # REQUIRED. Shown on the page and in link cards.
summary: A template node that demonstrates every field and directive.  # REQUIRED (one line).

# OPTIONAL. Faith starting points this node speaks to. Used to surface entry points.
# Common values: atheist, agnostic, protestant, non-practicing-catholic.
audiences:
  - agnostic

# OPTIONAL. Set true to surface this node as a top-level card on the home page.
isEntryPoint: false

# OPTIONAL. Directed links to other nodes (a graph — several nodes may link to one id).
branches:
  - to: does-god-exist           # REQUIRED per branch: target node id.
    label: "Does God exist?"     # OPTIONAL: link text (falls back to the target's title).
    prompt: A short nudge shown beneath the label.  # OPTIONAL.

# OPTIONAL. Ordered ids of child nodes rendered as a collapsible accordion on this page.
# Each id must resolve to its own content file (see example-section.md).
sections:
  - example-section
---

This paragraph is the Markdown body. Everything below the front matter is rendered
to HTML. Use normal Markdown: **bold**, *italic*, lists, `code`, blockquotes, and so on.

## External link

An external link opens another site in a new tab:
[Vatican website](https://www.vatican.va){target="_blank" rel="noopener noreferrer"}.

## Internal link

An internal link navigates to another node without a full page reload:
[Does God exist?](node/does-god-exist), or back to the [start](.).

## Embedded YouTube video

Media use custom-container directives: an opening `:::` fence and a bare closing `:::`
fence. The closing fence is required — it terminates the block.

::: youtube aqz-KE-bpKQ
:::

## Embedded PDF

::: pdf _content/Respondeo.Content/content/assets/sample.pdf
:::

## Buttons

Open a file in a new tab, or force a download by adding `| download`:

::: button _content/Respondeo.Content/content/assets/sample.pdf | Open PDF in new tab
:::

::: button _content/Respondeo.Content/content/assets/sample.pdf | Download PDF | download
:::
