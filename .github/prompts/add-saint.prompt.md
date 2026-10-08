---
mode: agent
description: Add a new saint to the Respondeo Discover corpus (Markdown + facets + manifest).
---

# Add a saint

Add a new saint profile to `src/Respondeo.Content/wwwroot/discover/saints/`.

If the saint, era, region, state(s) of life, or canonization status was not provided, ask before
proceeding.

Follow [`content-authoring.instructions.md`](../instructions/content-authoring.instructions.md) and
do all of the following:

1. Create `<slug>.md` with front-matter: `id` (= file stem), `title`, `sortKey` (drop a leading
   "St."/"The"), `summary`, `era`, `region`, `patronages` (list), `statesOfLife` (list),
   `canonizations` (list), optional `dates`, optional `feastDay`, `tags`, and `sources`
   (`{ label, url? }`).
2. Use only facet slugs that exist in `saints/facets.json`:
   - `era` (single): `early-church`, `medieval`, `early-modern`, `modern`
   - `region` (single): `europe`, `north-america`, `latin-america`, `africa`, `asia`, `middle-east`, `oceania`, `unknown`
   - `statesOfLife` (list, at least one): `religious`, `deacon`, `priest`, `bishop`, `pope`, `lay`, `martyr`, `virgin`, `widow`
   - `canonizations` (list, at least one): `canonized`, `beatified`, `venerable`, `servant-of-god`, `pre-congregation`, `doctor-of-the-church`
   - `patronages` is **free text**, not a facet slug — write display-ready values (e.g. `["Missions", "The poor"]`). Reuse existing wording/casing where one already exists for consistency.
3. `statesOfLife` and `canonizations` are multi-valued — a saint may hold several (e.g. a martyred
   priest is `[priest, martyr]`; a canonized Doctor is `[canonized, doctor-of-the-church]`). Both
   must be populated (enforced by test). List every state of life that applies — the slugs are
   independent descriptors (not a ranked ladder), and the filter matches any saint whose list
   *contains* a selected state. Holy orders are cumulative (`deacon` → `priest` → `bishop` → `pope`),
   so include each order held: a bishop is
   `[deacon, priest, bishop]` and a pope is `[deacon, priest, bishop, pope]`. Add `religious` for a
   vowed order member (not secular clergy), and `martyr`/`virgin`/`widow` whenever true — e.g. a
   friar-bishop is `[deacon, priest, bishop, religious]` and a deacon-only friar is `[deacon,
   religious]`. `patronages` is searchable/shown but is not a filter facet.
4. Write the body as `## `-sectioned Markdown (e.g. His/Her life / The Little Way / Why it matters).
   Present facts honestly; set `reviewStatus: unvetted` for AI-drafted or not-yet-reviewed profiles.
5. Register the file name in `saints/saints-manifest.json`.
6. Build and run the `Respondeo.UnitTests` Content tests; fix any failures.
