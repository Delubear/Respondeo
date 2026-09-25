# Authoring Content Nodes

This project holds the site's content, shipped as static web assets from the
`Respondeo.Content.Markdown` library. Each **node** is a single Markdown file made of two
parts:

1. **YAML front-matter** — structured metadata between two `---` lines.
2. **Markdown body** — the readable content, rendered to HTML and shown on the node page.

New files must be added to [`manifest.json`](manifest.json) to be loaded.

---

## 1. File structure

```markdown
---
id: does-god-exist
title: "Does God exist?"
summary: A short one-line description used on cards and previews.
topics:
  - Existence of God
  - Getting Started
branches:
  - to: what-is-faith
    label: "What do you mean by faith?"
    prompt: A natural next question to explore.
---

Your Markdown body starts here.
```

### Front-matter fields

| Field | Required | Description |
|---|---|---|
| `id` | Yes | Stable unique key used to link between nodes. Keep it URL-friendly (lowercase, hyphens). |
| `title` | Yes | Headline shown on the page and in link cards. |
| `summary` | No | One-line description used on cards and previews. |
| `topics` | No | Categories this node belongs to, used to group and filter articles (e.g. `Existence of God`, `St. Thomas Aquinas`). |
| `branches` | No | Outgoing links to other nodes (see below). |
| `sections` | No | Ids of section nodes rendered inline as a collapsible accordion on this page. |

### Branch links

Each entry under `branches` navigates to another node:

| Field | Required | Description |
|---|---|---|
| `to` | Yes | The `id` of the target node. |
| `label` | No | Override text for the link. Falls back to the target's `title`. |
| `prompt` | No | Short reason/question shown beneath the label. |

Multiple nodes may link to the same `to` id — the content forms a **graph**, not a tree.

---

## 2. Writing the body

The body is standard Markdown with
[Markdig advanced extensions](https://github.com/xoofx/markdig) enabled (tables, task lists,
auto-identifiers, generic attributes, etc.).

### Links

```markdown
Internal (no full reload): jump to [Does God exist?](node/aquinas-five-ways) or the [home](.).
External: visit the [Vatican website](https://www.vatican.va){target="_blank" rel="noopener noreferrer"}.
```

- **Internal** links use `node/<id>` and navigate within the app.
- **External** links should carry `{target="_blank" rel="noopener noreferrer"}` (generic-attribute
  syntax) so they open safely in a new tab.

### Summa citations

Link into the Summa Theologiae corpus with `summa/<url-id>` and an optional article anchor
(`#article-N`, `#article-N-objection-M`, or `#article-N-reply-M`). The visible citation text must
match the style the app's `SummaReferenceRenderer` produces for the Summa's own cross-references, so
hand-written references read identically: use `Q.` for a question, `A.` for an article, `ad` for a
reply, and `obj.` for an objection — never the lower-case `q.`/`a.` forms or the phrasing
`reply to obj.`

```markdown
[*Summa Theologiae* I, Q. 2, A. 3](summa/prima-q002#article-3)
[*Summa Theologiae* I, Q. 2, A. 3, ad 1](summa/prima-q002#article-3-reply-1)
```

Both the target and the citation style are checked at build time by `ContentLinkIntegrityTests`, so a
broken link or an off-style citation fails the tests.

### Headings, quotes, code, lists, images

Use normal Markdown (`##`, `>`, `` `code` ``, `-`, `![alt](src)`). These are styled by the scoped
CSS in `Pages/Node.razor.css`.

---

## 3. Embed directives (preferred over raw HTML)

For recurring embeds, use the **custom-container directives** instead of hand-writing HTML. They
expand to canonical, consistent markup (defined once in
[`Services/ContentContainerExtension.cs`](Services/ContentContainerExtension.cs)), so class
names and required attributes never drift.

The syntax is a fenced container: `:::` on its own opening line with the directive name and
arguments, then a closing `:::`.

### YouTube video

```markdown
::: youtube aqz-KE-bpKQ
:::
```

- Argument: the YouTube **video id** (the part after `watch?v=`).
- Renders a click-to-load facade (a lazy thumbnail plus a play button). The privacy-friendly
  (`youtube-nocookie`) player is only loaded when the visitor clicks play, so opening a page
  makes no requests to YouTube.

### PDF (inline viewer)

```markdown
::: pdf _content/Respondeo.Content.Markdown/content/assets/sample.pdf
:::
```

- Argument: the **path or URL** to the PDF.
- Renders an inline iframe using the browser's built-in PDF viewer.

### Button (link styled as a button)

```markdown
::: button _content/Respondeo.Content.Markdown/content/assets/sample.pdf | Open PDF in new tab
:::

::: button _content/Respondeo.Content.Markdown/content/assets/sample.pdf | Download PDF | download
:::
```

Arguments are `|`-separated:

| Position | Required | Description |
|---|---|---|
| 1 — href | Yes | The link target (path or URL). |
| 2 — text | No | Button label. Defaults to the href if omitted. |
| 3 — `download` | No | When present, the button downloads the file instead of opening it in a new tab. |

Without the `download` flag, the button opens in a new tab with `rel="noopener noreferrer"`.

### Raw HTML (escape hatch)

Directives cover the common cases. For genuine one-offs you can still write raw HTML directly in
the body — but if a pattern appears more than once, prefer adding a directive so it stays
consistent. Unknown `:::` container names fall back to a plain `<div>` with the name as a CSS class.

---

## 4. Adding a new node — checklist

1. Create `wwwroot/content/<id>.md` with front-matter and body.
2. Add the file name to [`manifest.json`](manifest.json).
3. Link to it from another node via a `branches` entry (`to: <id>`) or an inline
   `[label](node/<id>)` link.
4. Run the app and verify the page renders and links resolve.
