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
   optional `designations` (list), optional `sex` (single), optional `religiousOrders` (list),
   `canonizations` (list), optional `dates`, optional `feastDay`, `tags`, and `sources`
   (`{ label, url? }`).
2. Use only facet slugs that exist in `saints/facets.json`:
   - `era` (single): `early-church`, `medieval`, `early-modern`, `modern`
   - `region` (single): `europe`, `north-america`, `latin-america`, `africa`, `asia`, `middle-east`, `oceania`, `unknown`
   - `statesOfLife` (list, at least one): `religious`, `deacon`, `priest`, `bishop`, `pope`, `lay`
   - `designations` (list, optional): `apostle`, `evangelist`, `prophet`, `martyr`, `virgin`, `widow`, `founder`, `doctor-of-the-church`
   - `sex` (single, optional): `male`, `female`
   - `religiousOrders` (list, optional): `augustinian`, `dominican`, `franciscan`, `capuchin`, `carmelite`
   - `canonizations` (list, at least one): `canonized`, `beatified`, `venerable`, `servant-of-god`, `pre-congregation`
   - `patronages` is **free text**, not a facet slug — write display-ready values (e.g. `["Missions", "The poor"]`). Reuse existing wording/casing where one already exists for consistency.
3. `statesOfLife` and `canonizations` are multi-valued and required — a saint may hold several (e.g.
   a bishop is `[deacon, priest, bishop]`; a canonized saint is `[canonized]`). Both must be
   populated (enforced by test). List every state of life that applies — the slugs are
   independent descriptors (not a ranked ladder), and the filter matches any saint whose list
   *contains* a selected state. Holy orders are cumulative (`deacon` → `priest` → `bishop` → `pope`),
   so include each order held: a bishop is
   `[deacon, priest, bishop]` and a pope is `[deacon, priest, bishop, pope]`. Add `religious` for a
   vowed order member (not secular clergy). Use `designations` for honorifics/roles that are not
   states of life — `martyr`/`virgin`/`widow`, `apostle`/`evangelist`/`prophet`, `founder` (of an
   order/movement), and `doctor-of-the-church`. Use `religiousOrders` for the institute(s) a
   `religious` saint belonged to or founded (omit for secular clergy and for founders of non-order
   institutes such as a personal prelature; never tag an order founded after the saint's lifetime).
   Set `sex` to `male` or `female`. For example a friar-bishop Doctor is
   `statesOfLife: [deacon, priest, bishop, religious]`, `designations: [doctor-of-the-church]`,
   `religiousOrders: [dominican]`. `patronages` is searchable/shown but is not a filter facet.
4. Write the body as `## `-sectioned Markdown (e.g. His/Her life / The Little Way / Why it matters).
   Present facts honestly; set `reviewStatus: unvetted` for AI-drafted or not-yet-reviewed profiles.
5. Register the file name in `saints/saints-manifest.json`.
6. Build and run the `Respondeo.UnitTests` Content tests; fix any failures.
