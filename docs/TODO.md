# Respondeo — Content TODO & Topic Backlog

A running list of topics we are missing, considering, or want to deepen. 
Stubs exist in the content pipeline but are marked as unvetted; this file tracks the editorial intent behind them and the gaps still to fill.

## Legend

- [ ] Not started / stub only
- [~] In progress (partial real content)
- [x] Real, vetted content complete

---

## Stage: Why God?

- [x] Landing page
- [x] Can we even know the truth? (epistemology / relativism)
- [x] The Five Ways (overview + five arguments)
- [x] Foundations: act & potency, essence & existence
- [x] Objections: problem of evil, divine hiddenness, who made God, science explains it, burden of proof
- [x] What is God like? (divine attributes)
- [ ] Consider: Is the universe eternal? (does it change the argument?)
- [ ] Consider: Fine-tuning / contemporary design arguments
- [ ] Consider: Free will and determinism as a preamble

## Stage: Which God?

Bridges the gap from "a God" (natural theology) to "the personal, covenant God of Israel."
Structured as an ordered path (flow wired; nodes still stubs), with one optional detour:
monotheism → personal → can reveal → (other faiths, optional) → God of Israel → *Why Jesus?*

- [x] Landing page (reads as an ordered path, not a menu)
- [x] Why one God, not many? (monotheism vs. polytheism/dualism) — path start
- [x] Is the first cause personal? (mind & will vs. blind principle)
- [x] Could God speak to us? (possibility of revelation, motives of credibility)
- [x] What about other faiths? (Islam, deism, Eastern conceptions) — optional detour
- [x] Why the God of Israel? (covenant, prophets, ethical monotheism) — hands off to *Why Jesus?*
- [x] Consider: The Trinity as a later revealed refinement (forward pointer)

## Stage: Why Jesus?

Structured as an ordered path (flow wired):
who was Jesus → trust the Gospels → claimed to be God → (OT prophecy, optional) → the Resurrection → (objections, optional) → founded a Church → *Why the Church?*

- [x] Landing page (reads as an ordered path, not a menu)
- [x] Who was Jesus? (historical man + the Incarnation claim) — path start
- [x] Can we trust the Gospels? (dating, eyewitness roots, manuscripts)
- [x] Jesus' divine claims
- [x] Did Jesus fulfill prophecy? (messianic expectation) — optional detour
- [x] The Resurrection (historical case)
- [x] Objections to Jesus (accordion: legend theory, resurrection alternatives, gospel contradictions) — optional detour
- [x] Jesus founded a Church (slimmed to a bridge: the risen Christ left a *community*, not just a memory; full case now lives in *Why the Church?*) — hands off to *Why the Church?*
- [x] Summa citations integrated into the why-Jesus spine (Incarnation III Q.1 A.1; Resurrection III Q.55 A.6; Head of the Church III Q.8 A.1)
- [x] Consider: The "Liar, Lunatic, Lord, or Legend" trilemma in full
- [x] Consider: Resurrection minimal-facts sub-pages (empty tomb, appearances, origin of belief)

## Stage: Why the Church?

Structured as a single ordered spine with one optional detour:
hub → founded by Christ → recognizing it → four marks → authority → (objections, optional) → where the path has led → *Coming Home*

- [x] Landing page (single-entry hub)
- [x] The Church founded by Christ
- [x] Recognizing the true Church
- [x] The four marks (one, holy, catholic, apostolic)
- [x] Scripture, Tradition, and authority (Magisterium)
- [x] Objections hub (accordion) — optional detour
  - [x] Sola scriptura
  - [x] Scandals / holiness
  - [x] The Reformation
  - [x] Papacy (rock & keys)
  - [x] Eastern Orthodoxy (which apostolic church?)
  - [x] The Blessed Virgin Mary (role in salvation, the New Ark)
  - [x] Praying to the saints
  - [x] Science, evolution, and Genesis (does the faith require a young earth?)
- [x] Where the path has led (consolidating terminus) — hands off to *Coming Home*

## Stage: Coming Home

Audience-based hub. Three persona entry paths converge on *What the Church offers*, which (with the
returning-Catholic path) fans out to two optional, cross-linked detours — *Why practice* and *What holds
people back* — both of which lead to *How to begin*, the stage terminus that hands off to Discover.
Flow graph audited: no unreachable nodes, single intended terminus.

- [x] Landing page (audience-based hub)
- [x] If you're not yet Christian (entry path) → What the Church offers
- [x] If you're Christian but not Catholic (entry path) → What the Church offers
- [x] Returning to the faith (cradle Catholic or later-received, now non-practicing) (entry path)
- [x] What the Church offers (convergence point → why practice / obstacles / how to begin)
- [x] Why practice, and why believe it for real (optional detour, cross-linked with obstacles)
- [~] What holds people back (obstacles: shame, wounds, habit, doubt, cost) (optional detour, cross-linked with why practice)
- [~] How to begin (sections: returning Catholic, entering from outside, Eastern Orthodox) — hands off to *Discover*

---

## Cross-cutting / possible future stages

- [~] Possible 6th stage: "Living It" — sacraments, prayer, daily practice
- [ ] Suffering (fuller pastoral treatment beyond the problem-of-evil objection)
- [ ] Morality without God / grounding of objective moral values
- [ ] Science & faith (history, Galileo, evolution) as a standalone thread
- [ ] Glossary of key terms (act/potency, contingency, hypostatic union, etc.)
- [ ] Reading list / primary-source references per stage

## Discover area: Miracles

An area within the Discover pillar (reached from the Discover sub-navigation, routes under
`/discover/miracles/`), gated by the `MiraclesFeature` flag. Each miracle is a hand-authored Markdown
file with typed facet front-matter (type, approval, region, country, year, tags), parsed into a typed
model and browsable by facet + free-text search.

- [~] Miracles area scaffolding (models, service, browse state, pages, Discover sub-nav link, feature flag)
- [~] Browse page: facet filters (kind / judgment / region) + search + sort by title/year
- [~] Detail page: facts panel, prose sections, tags, sources
- [~] The Eucharistic Miracle of Lanciano
- [~] The Eucharistic Miracle of Buenos Aires
- [~] Our Lady of Guadalupe
- [~] The Cures of Lourdes
- [~] The Incorrupt Body of St. Bernadette
- [~] Our Lady of Soufanieh (Damascus apparitions to Myrna Nazzour)
- [ ] Consider: theological/philosophical treatment of what a miracle is (Aquinas, Hume)
- [ ] Consider: more Eucharistic miracles (Orvieto-Bolsena, Santarém, Sokółka)
- [ ] Consider: more approved apparitions (Lourdes 1858, Fatima, Knock, Akita)
- [ ] Consider: canonization miracles / more incorruptibles

## Discover area: Saints

An area within the Discover pillar (reached from the Discover sub-navigation, routes under
`/discover/saints/`), gated by the `SaintsFeature` flag. Each saint is a hand-authored Markdown file
with typed facet front-matter (era, region, patronages, states of life, designations, sex, religious
orders, canonizations, dates, feast day, tags), parsed into a typed model and browsable by facet +
free-text search.

- [x] Saints area scaffolding (models, service, browse state, pages, Discover sub-nav link, feature flag)
- [x] Browse page: facet filters (era / region / state of life / canonization) + search + sort by title
- [x] Detail page: facts panel (with era description), prose sections, patronages, tags, sources
- [x] St. Augustine of Hippo
- [x] St. Francis of Assisi
- [~] St. Thérèse of Lisieux
- [ ] Consider: more Doctors of the Church and broader regional/era coverage
- [ ] Consider: patronage grouping once the corpus is large enough to warrant it

## Pillar: Discover

The "living the faith" pillar (label/route `Discover` / `/discover/`), reached from the masthead and from the
end of Coming Home as an ongoing endgame rather than a new journey stage. Prayers and articles are
hand-authored Markdown; devotions are data-driven JSON so an interactive Rosary/chaplet player can walk
the visitor bead by bead without code changes.

- [x] Discover pillar scaffolding (project, models, parser, services, manifests, DI, masthead link, feature flags)
- [x] Hub page (`/discover`) linking Miracles, Saints, Prayers, Devotions, Articles
- [x] Prayers: searchable index (`/discover/prayers`) + detail with optional Latin (`/discover/prayers/{id}`)
- [x] Devotions: index (`/discover/devotions`) + interactive data-driven player (`/discover/devotions/{id}`)
- [x] Articles: index (`/discover/articles`) + sectioned detail with sources (`/discover/articles/{id}`)
- [x] Coming Home finale hands off into Discover via `nextStage`
- [ ] Consider: more prayers (Litanies, the Angelus, Divine Mercy), more chaplets
- [ ] Consider: more deep-dive articles (the Eucharist, Confession practice, the liturgical year)
- [ ] Consider: mind copyright on any non-public-domain prayers/translations

## Future / possible pillars

Additional standalone, searchable pillars (like the Summa, or the Miracles/Saints browse areas) that
would fit the same browse-by-facet + Summa-style reading layout. Not being pursued for now; captured
here as editorial intent.

- [ ] **The Bible** — a browsable Scripture pillar (books → chapters → verses) with cross-references,
  mapping naturally onto the Summa-style navigation. Would need a public-domain translation
  (e.g. Douay–Rheims / Vulgate) to ship freely. *Note:* the Douay–Rheims Bible is in the public
  domain, so it can be shipped freely.
- [ ] **The Catechism of the Catholic Church** — its numbered, cross-referenced paragraphs would map
  onto the Summa-style layout very well. *Not now:* the current English translation is still under
  copyright and cannot be shipped in this repository; revisit if a suitably licensed or public-domain
  text becomes available.

## Housekeeping

- [ ] Vet "unvetted content" stubs and remove the NOTE banner as they are finished

## Technical / SEO

### Proposal: build-time prerendering (SSG) for crawlers, keeping the WASM-first approach

- [ ] **Prerender each route to static HTML at build time, so non-JS crawlers and social
  unfurlers get per-page markup instead of the generic `index.html` shell.**

**Why.** The site is a pure Blazor WebAssembly app: every URL serves the same static
`wwwroot/index.html`, and the real per-page content (title, description, `SeoHead` tags,
JSON-LD) only materializes after the browser boots WASM and renders client-side. That splits
visitors in two:

- **JS-capable crawlers (Google, Bing)** run the app and already see full per-route metadata and
  structured data. SEO for the engines that matter most is effectively fine today.
- **Non-JS scrapers (some social-link unfurlers, smaller/non-JS bots)** only ever see the static
  shell, so every URL shows the same site-level title/description/preview card.

This is a *documented, accepted* trade-off (see the comment in `Components/SeoHead.razor` and
`wwwroot/index.html`): we keep a simpler, static-hosted, fully offline/PWA architecture in exchange
for weaker previews on non-JS consumers. The static baseline in `index.html` is already as strong
as a single shell can be (complete OG + Twitter + canonical + description, matching `SiteMeta`).
The only thing it cannot be is *per-page* — which is exactly what prerendering would add.

**Approach (the one architecture-compatible option).** Because the site deploys to **GitHub Pages
(static hosting only)**, server-side prerender hosts (`WebAssemblyPrerendered`, a render host) are
out. The fit is **build-time static snapshots**:

1. Reuse the existing route enumeration in `tools/Respondeo.SitemapGenerator` (it already walks
   every content manifest + the Summa catalog into a `RouteSet` and matches real app routes), so the
   prerender list and `sitemap.xml` stay in lockstep by construction.
2. Serve a locally-published production build.
3. Use a headless browser (Playwright/Puppeteer) to visit each route — running the real WASM app —
   and save the hydrated DOM to a per-route `…/index.html` (needed for GitHub Pages per-URL routing).
4. Wire the prerender step into the publish pipeline, after sitemap generation.

**Why it preserves WASM-first.** Each snapshot keeps the normal Blazor bootstrap: browsers hydrate
into the live WASM app exactly as today; crawlers/unfurlers read the baked markup. Hosting stays
static (no server, no cache-header needs).

**Costs / constraints to weigh.**

- Adds a Node + headless-browser dependency and extra publish time (hundreds of routes to render).
- Requires emitting per-route output files and handling GitHub Pages SPA routing (per-route
  `index.html`, or the `404.html` fallback trick).
- Concrete payoff is mainly **better social link previews + non-JS bots**, since Google/Bing already
  execute the app's JS. Decide whether that payoff justifies the pipeline complexity.

**Related, already done (for context).**

- [x] Per-page `SeoHead` (title, description, canonical, Open Graph, Twitter Card, JSON-LD).
- [x] Static `index.html` baseline for non-JS scrapers (full OG/Twitter/canonical, matches `SiteMeta`).
- [x] `sitemap.xml` + `robots.txt` generated from content manifests at build time.
- [x] Saint detail pages emit a schema.org `Person` JSON-LD node (`SiteMeta.PersonJsonLd`) rather than
  a generic `Article`.
- [ ] Consider: richer JSON-LD types for other pillars if a clear schema.org fit exists (most are
  reasonable as `Article`/`CreativeWork` today).
