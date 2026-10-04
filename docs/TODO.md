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
- [x] Sacraments content relocated out of Coming Home into the Discover pillar

---

## Cross-cutting / possible future stages

- [ ] Possible 6th stage: "Living It" — sacraments, prayer, daily practice
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
- [ ] Consider: theological/philosophical treatment of what a miracle is (Aquinas, Hume)
- [ ] Consider: more Eucharistic miracles (Orvieto-Bolsena, Santarém, Sokółka)
- [ ] Consider: more approved apparitions (Lourdes 1858, Fatima, Knock, Akita)
- [ ] Consider: canonization miracles / more incorruptibles

## Pillar: Discover

The "living the faith" pillar (label/route `Discover` / `/discover/`), reached from the masthead and from the
end of Coming Home as an ongoing endgame rather than a new journey stage. Prayers and articles are
hand-authored Markdown; devotions are data-driven JSON so an interactive Rosary/chaplet player can walk
the visitor bead by bead without code changes.

- [~] Discover pillar scaffolding (project, models, parser, services, manifests, DI, masthead link, feature flags)
- [~] Hub page (`/discover`) linking Miracles, Prayers, Devotions, Articles
- [~] Prayers: searchable index (`/discover/prayers`) + detail with optional Latin (`/discover/prayers/{id}`)
- [~] Devotions: index (`/discover/devotions`) + interactive data-driven player (`/discover/devotions/{id}`)
- [~] Articles: index (`/discover/articles`) + sectioned detail with sources (`/discover/articles/{id}`)
- [~] Coming Home finale hands off into Discover via `nextStage`
- [x] Relocate the Sacraments content into Discover articles
- [ ] Consider: more prayers (Litanies, the Angelus, Divine Mercy), more chaplets
- [ ] Consider: more deep-dive articles (the Eucharist, Confession practice, the liturgical year)
- [ ] Consider: mind copyright on any non-public-domain prayers/translations

## Future / possible pillars

Additional standalone, searchable pillars (like the Summa, or the Miracles/Saints browse areas) that
would fit the same browse-by-facet + Summa-style reading layout. Not being pursued for now; captured
here as editorial intent. The Saints is the most likely next Discover area if any is revisited.

- [ ] **The Bible** — a browsable Scripture pillar (books → chapters → verses) with cross-references,
  mapping naturally onto the Summa-style navigation. Would need a public-domain translation
  (e.g. Douay–Rheims / Vulgate) to ship freely. *Note:* the Douay–Rheims Bible is in the public
  domain, so it can be shipped freely.
- [ ] **The Catechism of the Catholic Church** — its numbered, cross-referenced paragraphs would map
  onto the Summa-style layout very well. *Not now:* the current English translation is still under
  copyright and cannot be shipped in this repository; revisit if a suitably licensed or public-domain
  text becomes available.
- [ ] **The Saints** — a searchable pillar of saints with typed facets (era, region, feast day,
  patronage, state of life) and hand-authored profiles, close in shape to the Miracles area.

## Housekeeping

- [ ] Vet "unvetted content" stubs and remove the NOTE banner as they are finished
