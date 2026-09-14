---
id: test
title: "Test"
summary: A sandbox for trying out different content types.
isEntryPoint: true
branches:
  - to: does-god-exist
    label: "Internal branch link (does-god-exist)"
    prompt: Branch links are the built-in way to navigate between nodes.
---

This node exists purely to experiment with the kinds of content we can embed.
Nothing here is theological — it is a playground.

## External link

An external link opens another site.
Here is one to the [Vatican website](https://www.vatican.va){target="_blank" rel="noopener noreferrer"}.

## Internal link

An internal link navigates to another node within Respondeo without a full page reload.
For example, jump to [Does God exist?](node/does-god-exist) or back to the [start](.).

## Embedded YouTube video

::: youtube aqz-KE-bpKQ
:::

## PDF integration

**Embedded viewer** — the PDF is shown inline using the browser's built-in viewer:

::: pdf _content/Respondeo.Content/content/assets/sample.pdf
:::

**Buttons** — open the PDF in a new tab or download it:

::: button _content/Respondeo.Content/content/assets/sample.pdf | Open PDF in new tab
:::

::: button _content/Respondeo.Content/content/assets/sample.pdf | Download PDF | download
:::
