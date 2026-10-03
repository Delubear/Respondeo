---
mode: agent
description: Add a new devotion to the Respondeo Discover corpus (JSON sequence + manifest).
---

# Add a devotion

Add a new data-driven devotion to `src/Respondeo.Content/wwwroot/discover/devotions/`.

If the devotion, its prayer sequence, or mystery sets were not provided, ask before proceeding.

Follow [`content-authoring.instructions.md`](../instructions/content-authoring.instructions.md) and
do all of the following:

1. Create `<slug>.json` with top-level `id` (= file stem), `title`, `sortKey`, `summary`, `kind`
   (e.g. `rosary` | `chaplet` | `litany`), `intro`, optional `mysterySets`, and the required ordered
   `sequence`.
2. **Every `prayerId`** referenced in `sequence` / `perMystery` must already exist in
   `discover/prayers/`. If a prayer is missing, add it first (see add-prayer) and register it.
3. Use valid `bead` markers on steps: `cross`, `medal`, `large`, `small`, `between` (omit for
   non-bead steps).
4. Register the file name in `devotions/devotions-manifest.json`.
5. Build and run the `Respondeo.UnitTests` Content tests; fix any failures.
