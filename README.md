# Respondeo

A Blazor WebAssembly app that presents a graph of Markdown-authored content nodes.

## Authoring content

Content lives in `Respondeo.Content/wwwroot/content/` as Markdown files, each with a YAML
front-matter header followed by a Markdown body. New files must also be listed in
`manifest.json` so `ContentService` can load them.

Two ready-to-copy templates live alongside the content (they are intentionally left out of
`manifest.json`, so they are not served as live pages — copy them to start a new file):

- [`example.md`](Respondeo.Content/wwwroot/content/example.md) — a full node stub showing
  every front-matter field and an example of each parser directive.
- [`example-section.md`](Respondeo.Content/wwwroot/content/example-section.md) — a minimal
  section stub for use as a collapsible section of a parent page.

### Front matter

```yaml
---
id: test                       # unique node id (also the URL: /node/test)
title: "Test"                  # display title
summary: A short description.  # shown in cards and link previews
isEntryPoint: true             # optional: surfaces the node on the home page
branches:                      # optional: links to other nodes
  - to: does-god-exist         # target node id
    label: "Does God exist?"   # link text
    prompt: A short nudge.      # optional descriptive line
sections:                      # optional: ids of section nodes for an accordion
  - five-ways-motion
---
```

### Links

- External link (opens a new tab):
  `[Vatican website](https://www.vatican.va){target="_blank" rel="noopener noreferrer"}`
- Internal link (no full page reload): `[Does God exist?](node/does-god-exist)`

### Media directives (custom containers)

Rich embeds use Markdig's *custom-container* syntax, which is a **block** delimited by a
pair of `:::` fences — an opening fence and a closing fence, just like the triple backticks
of a fenced code block:

```text
::: youtube aqz-KE-bpKQ
:::
```

- The **first `:::` line opens** the container. The word after it (`youtube`) selects the
  directive, and the rest of the line (`aqz-KE-bpKQ`) is its argument.
- The **second, bare `:::` line closes** the container. It is required — it terminates the
  block. Without it, the parser treats everything to the end of the file as still being
  inside the container and swallows the rest of the page.

The directives below do not use any body text between the fences (the markup is built from
the argument alone), but the closing `:::` is still mandatory to bound the block. The paired
fences are also what keep authoring consistent across every directive.

| Directive | Syntax | Renders |
| --- | --- | --- |
| YouTube | `::: youtube <videoId>` | A lazy-loaded, privacy-friendly embedded player |
| PDF | `::: pdf <path>` | An inline PDF viewer |
| Button | `::: button <href> \| <text>` | A link button that opens in a new tab |
| Button (download) | `::: button <href> \| <text> \| download` | A link button that downloads the file |

Examples:

```text
::: youtube aqz-KE-bpKQ
:::

::: pdf _content/Respondeo.Content.Markdown/content/assets/sample.pdf
:::

::: button _content/Respondeo.Content.Markdown/content/assets/sample.pdf | Open PDF in new tab
:::

::: button _content/Respondeo.Content.Markdown/content/assets/sample.pdf | Download PDF | download
:::
```

Any unrecognized container name falls back to Markdig's default `<div>` rendering. The
directive vocabulary is defined in `Services/ContentContainerExtension.cs`.
