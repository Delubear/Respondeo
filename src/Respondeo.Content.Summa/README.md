# Summa Theologiae Content (Generated)

This project ships the bundled **Summa Theologiae** corpus as static web assets from the
`Respondeo.Content.Summa` library. Unlike the other content pillars, **you do not hand-author files
here.** Every file under `wwwroot/summa/` is produced by the
[`Respondeo.SummaImporter`](../../tools/Respondeo.SummaImporter) tool from a plain-text source of the
Summa, so edits made directly to these JSON files would be overwritten on the next import.

---

## 1. What ships here

| Path | Purpose |
|---|---|
| `wwwroot/summa/summa-index.json` | The lightweight browse/search index (parts → questions), fetched once. |
| `wwwroot/summa/<part-folder>/<id>.json` | The full content of a single question, fetched on demand. |

The corpus is split into one subfolder per part so no single directory holds the whole corpus:

| Part id | Folder |
|---|---|
| `p1` | `first-part` |
| `p2a` | `first-part-of-the-second-part` |
| `p2b` | `second-part-of-the-second-part` |
| `p3` | `third-part` |
| `sup` | `supplement` |

`SummaService` derives the folder from a question's part-key prefix (the part before the first `-` in
the `id`) via the shared `SummaParts` registry, so the on-disk layout stays independent of the URL
slug and display label.

---

## 2. Regenerating the content

Run the importer, pointing it at a plain-text source of the Summa and this project's `wwwroot`:

```powershell
dotnet run --project tools/Respondeo.SummaImporter -- <path-to-summa.txt> src/Respondeo.Content.Summa/wwwroot
```

This emits `summa/summa-index.json` plus one `summa/<part-folder>/<id>.json` per question. The tool
also renders the prose to HTML (via Markdig) and resolves the Summa's own cross-references, so the
citation style stays consistent with hand-written references elsewhere in the app.

To change how content is parsed, rendered, or cross-referenced, edit the importer's parser
(`tools/Respondeo.SummaImporter/SummaParser.*.cs`) and re-run it — never the generated JSON.

---

## 3. Consuming the content

Blazor pages depend only on `ISummaService`, which loads the index once and fetches each question's
full content on demand, caching both. The bundled assets are fingerprinted per deploy, so the
service caches them aggressively without risk of serving stale content. See the Summa browse and
question pages in the main app for usage.
