---
# ─────────────────────────────────────────────────────────────────────────────
# EXAMPLE SECTION FILE — a node used as a collapsible section of a parent page.
# A section file is just an ordinary content node; a parent lists its `id` under
# `sections:` (see example.md). The parent renders this file's `title` as the
# section header and this body as the expandable panel.
# After copying: give it a unique `id` and add the file name to manifest.json.
# ─────────────────────────────────────────────────────────────────────────────

id: example-section              # REQUIRED. Unique id; referenced from a parent's sections list.
title: "Example Section"         # REQUIRED. Becomes the accordion section header.
summary: A short description of this section.  # REQUIRED (one line).
---

This is the body of a section. It becomes the expandable panel shown when the
visitor opens this section in the parent page's accordion. Author it with normal
Markdown — paragraphs, lists, blockquotes, and the same media directives available
in any content file.
