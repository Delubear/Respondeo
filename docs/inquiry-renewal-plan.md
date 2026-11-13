# Inquiry renewal plan

Date: 2026-11-12

Status: Implementation ready for user review, with changes left uncommitted. Independent specialist review, representative reader studies, and native-device verification remain pending.

Sections 1-11 preserve the original examination and implementation rationale; statements about the original application describe that baseline, not the revised application.
Section 12 records the implementation, verification evidence, and remaining limitations. Implementation is not doctrinal approval or independent certification.

## 1. Purpose and governing decision

Respondeo's primary purpose is to invite people into the Catholic Church through serious, accessible reasoning, drawing especially on St. Thomas Aquinas and the Summa Theologiae.
Prayer, Scripture reading, the calendar, devotions, and the Liturgy of the Hours support understanding and Catholic life; they are not the product's organizing purpose.

The recommendation is to strengthen the existing Inquiry journey rather than replace it with a new application, curriculum engine, or collection of unrelated features.
The current structure already serves the mission. The largest opportunities concern the quality of the reasoning, its supporting evidence, and the reader's ability to examine it.

A successful Inquiry experience lets a reader:

1. Recognize a question or objection they actually hold.
2. Understand the proposed answer without first learning an entire philosophical vocabulary.
3. Identify the premises, evidence, and limits of the argument.
4. Examine the strongest relevant objections and the original sources.
5. Continue toward Catholic belief or examine an unresolved premise without losing their place.
6. Find a concrete, freely chosen next step toward Catholic life when ready.

The goal is conviction through a credible case, not a browsing-completion score or an interface that assumes assent.
Clarity about a disputed premise is progress even when the reader does not yet accept the conclusion.

## 2. Scope, evidence, and limitations

### Examination completed

The preceding examination covered all 69 shipped Inquiry Markdown articles, their manifest and flow data, and the principal content, navigation, and Summa-reading surfaces.
Graphify located code relationships; the source examination verified the relevant behavior. The graph did not substitute for reading the articles.

| Stage | Articles | Linked Summa references | Present role |
|---|---:|---:|---|
| Why God? | 17 | 25 | Truth, metaphysical foundations, Five Ways, attributes, and objections |
| Which God? | 11 | 4 | Unity, personality, revelation, comparison of religions, and Israel |
| Why Jesus? | 14 | 3 | Sources, divine claims, Trinity, prophecy, Resurrection, and commission |
| Why the Church? | 16 | 1 | Continuity, marks, teaching authority, Catholic objections, and decision |
| Coming Home | 11 | 0 | Starting situations, obstacles, sacramental life, and practical beginnings |
| Total | 69 | 33 | A substantial existing conversion journey |

Citation counts describe links, not distinct passages, evidential sufficiency, or article quality. More quotations are not automatically an improvement.

Sixteen targeted content, navigation, and flow integrity tests passed during the examination.
They establish the tested structural contracts, not the truth of an argument, its fairness, or a successful live reading experience.

### Important limits

- This is an editorial and product plan, not independent historical, philosophical, theological, or canonical certification.
- Proposed corrections identify where arguments need support or qualification; they do not declare every contested interpretation conclusively settled.
- External documentary sources and scholarly positions must be acquired and checked during implementation before claims are rewritten around them.
- The original examination was source-level, not a live browser, Android, accessibility, or first-time-reader usability study. Section 12 records subsequent browser verification.
- At examination time, all 69 articles omitted `reviewStatus`, and the parser treated omission as not unvetted. That did not establish completed review; implementation corrects this default.
- Existing calendar and Hours authority limitations remain unchanged. This plan neither certifies nor removes those features.

### Explicitly outside this effort

- A database, generic argument/rules engine, CMS, account system, or mandatory learning course.
- An AI apologetics chatbot or automated adjudication of a reader's beliefs.
- Tracking religious beliefs, conversion status, or individual objections through analytics.
- Rewriting the entire Discover catalog, Bible reader, or Summa corpus.
- Further calendar/Hours expansion as a prerequisite for improving Inquiry.
- A site-wide visual rebrand before the revised Inquiry experience has been tested.

## 3. What to preserve

1. **The five stages and their public identities.** They provide a coherent progression and allow readers to begin where they stand.
2. **The question-led home-page invitation.** The current home page already prioritizes reasoning rather than a dashboard of devotional utilities.
3. **Substantial objection content.** Preserve dedicated replies and the invitation to challenge the case.
4. **Existing conceptual groundwork.** Act/potency, essence/existence, and present causal dependence are already explained; refine rather than duplicate them.
5. **The boundary between reason and revelation where it is already stated well.** Extend that discipline consistently across stage transitions.
6. **Serious Trinity treatment and qualified prophecy discussion.** Do not flatten these into slogans.
7. **Recognition of other Christians' grace and Orthodox sacramental life.** Strengthen accuracy and charity rather than treating every alternative as equivalent.
8. **Differentiated practical entry guidance.** A returning Catholic, an unbaptized adult, and an Orthodox Christian do not have the same circumstances.
9. **Repository-authored Markdown and declarative flow data.** The content/flow/reader separation is appropriate and should remain simple.
10. **Stable routes and deep links.** Revision should preserve article IDs wherever possible, including shared accordion and Summa links.

## 4. Editorial standard

### 4.1 Flexible article pattern

Use this pattern as an editorial checklist, not a mandatory template with eight headings on every page:

1. The question in the reader's language.
2. A short answer that identifies the claim without pretending the argument has already been accepted.
3. Definitions needed for this specific argument, introduced close to use.
4. The argument, with premises and inferential steps visible.
5. The strongest relevant objections and replies.
6. What the argument establishes, what it depends on, and what it does not establish.
7. Sources with enough context to explain their evidential role.
8. Meaningful next questions, including a route for examining a disputed premise.

Use a short main explanation with optional depth. Do not make every page longer merely to satisfy the checklist.
Retain an explicit numbered argument where it helps; avoid making ordinary narrative sound like a formal demonstration when it is not one.

### 4.2 Distinguish kinds of reasoning

| Kind | How to present it | What not to imply |
|---|---|---|
| Philosophical demonstration | State premises, definitions, and why the conclusion follows | An illustration proves a disputed premise |
| Historical inference | Identify sources, dating, independence, limitations, and competing explanations | Textual preservation alone establishes historical accuracy |
| Doctrinal exposition | Identify the teaching and its authoritative context | A quotation is sufficient evidence for someone who rejects that authority |
| Fittingness or analogy | Explain what it illuminates and where the comparison stops | Something fitting must therefore have happened |
| Practical invitation | Explain a voluntary next step and what it involves | Reading an argument establishes assent or a personal obligation to enroll |

These distinctions should normally appear in natural prose, not a new taxonomy of technical badges.
Do not present all conclusions as equally uncertain. Defend conclusions confidently where warranted, but make the warrant visible.

### 4.3 Fairness and reader agency

- Address serious versions of opposing views, including confessional Protestant and Orthodox accounts, rather than only popular caricatures.
- Do not infer dishonesty, emotional avoidance, or bad faith merely because a reader remains unconvinced.
- Distinguish convinced-but-hesitant readers from readers who judge a premise insufficient.
- Do not describe an objection as answered more completely than the reply demonstrates.
- Separate institutional responsibility and victims' safety from analogies about individual sin.
- Prayer may be offered as a voluntary exploration, never as a replacement for examining historical evidence or an implied diagnostic of sincerity.
- Preserve the Catholic identity and invitation. Respectful argument does not require pretending the competing conclusions are interchangeable.

### 4.4 Sources and review

Keep three roles distinct: Aquinas explaining an argument, historical evidence supporting an event, and authoritative documents explaining Catholic teaching.
For major factual claims, identify the source, locator, context, and limits. A video may supplement the written case but must not be its only usable support.

A source review must check that the cited passage supports the adjacent claim, not merely that the URL or article anchor exists.
Quotes, translations, and reproductions require appropriate rights; use brief quotations and links rather than importing protected works wholesale.

Treat publication and review as separate facts. Missing metadata must not silently mean approved.
Start with a small explicit review-status vocabulary, validated at authoring time; distinguish an editorial pass from specialist or doctrinal review.
Do not introduce a universal approval badge or fabricate reviewer names, credentials, dates, source verification, or Church endorsement.
Record verified reviewer/date/scope details only when an actual review has occurred.

## 5. Content change register

Paths in this section are relative to `src\Respondeo.Content\wwwroot\inquiry`.
Grouped files share a coordinated revision; each retains a distinct purpose. New filenames below are proposals, not existing assets or approved final titles.

### 5.1 Why God? - first complete editorial example

**Why first:** This is the natural beginning, has the densest Summa integration, and can establish the standard before broader rewrites.

- `why-god\why-god.md`: Retain the invitation and existential relevance. Remove implications that remaining atheist or agnostic necessarily indicates dishonesty.
  Explain the stage's goal and the difference between examining arguments and already accepting them.
- `why-god\can-we-know-truth.md`: Preserve its foundational role. Clarify the limited task of establishing that reasoning is possible without claiming every objection to knowledge is settled.
- `why-god\aquinas-five-ways.md`: Keep the compact overview and map of conceptual prerequisites. Offer a recommended starting argument without requiring all five for continuation.
- `why-god\five-ways-act-potency.md`: Retain the groundwork. Distinguish definitions from defended metaphysical commitments; use examples without treating them as proof of the whole system.
- `why-god\five-ways-essence-existence.md`: Explain the distinction plainly and identify its argumentative role. Do not silently substitute it for Aquinas's Third Way.
- `why-god\five-ways-motion.md` and `five-ways-causation.md`: Preserve the distinction between present dependence and a temporal beginning.
  Explain why the examples fit an essentially ordered series, or label them as illustrations rather than sufficient demonstrations.
- `why-god\five-ways-contingency.md`: Highest-priority philosophical repair. Reconstruct the debated step from individual contingency to the claimed absence of all beings.
  Consult the primary text and credible interpretation. Either defend the original inference explicitly or distinguish a contemporary contingency argument from it.
  Correct the numbering while revising the full argument; do not hide the inferential problem through formatting alone.
- `why-god\five-ways-gradation.md`: Explain the relevant account of participation and perfection. Avoid assuming that ordinary comparative adjectives alone entail a maximal being.
- `why-god\five-ways-governance.md`: Make the bridge from natural directedness to intellect explicit. Distinguish teleology from a gap in current scientific explanation.
- `why-god\what-is-god-like.md`: Map each attribute to the reasoning supporting it. Distinguish what has been argued from properties still requiring further treatment.
- `why-god\objections-to-god.md`: Preserve the hub. Help readers select the disputed premise rather than suggesting every objection is a single alternative path to be completed.
- `why-god\objection-problem-of-evil.md`: Separate logical and evidential formulations, moral and natural suffering, and partial defenses from a complete theodicy.
  Revise the overbroad claim that freedom requires a world with sin; retain humane acknowledgment of suffering.
- `why-god\objection-divine-hiddenness.md`: Engage sincere nonresistant nonbelief. Do not assume convincing evidence would coerce love or that longing establishes its object's existence.
- `why-god\objection-who-made-god.md`: Preserve the distinction between everything having a cause and the argument's actual causal principle. State why God is not an arbitrary exemption.
- `why-god\objection-science-explains.md`: Preserve complementary explanatory levels. Avoid dismissing scientific explanations or making God dependent on unresolved scientific questions.
- `why-god\objection-burden-of-proof.md`: Affirm the author's responsibility to support positive claims. Distinguish withholding assent from asserting an alternative metaphysical explanation.

**Observed examples to revisit:** contingency lines 19-27; motion lines 69-81; causation lines 70-87; governance lines 50-58; hiddenness lines 16-23.
Line references describe the examined revision and may move during implementation.

**Completion criteria:** Each major argument states its premises and limits; conceptual support is available without forcing every reader through every Way.
An unresolved objection can be followed without losing the main route. Summa citations support the actual claim attributed to Aquinas.

### 5.2 Which God? - bridge from metaphysics to revelation

**Why:** Proving or arguing for classical theism does not by itself identify a historical revelation.

- `which-god\which-god.md`: State what the preceding stage is taken to establish and what this stage still needs to examine. Support readers entering directly.
- `which-god\which-god-monotheism.md`: Keep unity's place in the sequence. Show the relevant premises instead of treating many finite gods as the only rival account.
- `which-god\which-god-personal.md`: Expand the bridge to intellect and will. Explain analogical predication and avoid equating divine personality with human psychology.
- `which-god\which-god-can-reveal.md`: Separate possibility of revelation from evidence that a claimed revelation occurred.
  Qualify the claim that prophecy, miracles, and holiness are signs no human source could counterfeit; describe how authenticity would be examined.
- `which-god\which-god-other-faiths.md`: Define comparison criteria before applying them. Limit generalizations about Eastern traditions to identified positions.
  State the scope of comparison; this article need not pretend to settle every religious tradition.
- `which-god\which-god-of-israel.md`: Preserve its explicit boundary between reason, historical evidence, and faith.
  Source historical claims, distinguish checkability from confirmation, and explain why the evidence merits examining Christ rather than declaring the entire case complete.
- `which-god\which-god-objections.md`: Retain the hub and identify which question each reply actually addresses.
- `which-god\which-god-objection-all-same.md`: Distinguish contradictory truth claims from shared moral or spiritual insights without caricaturing religious pluralism.
- `which-god\which-god-objection-ot-violence.md`: Source claims about genre, conquest, and servitude. Separate textual interpretation from the moral question; avoid a quick dismissal.
- `which-god\which-god-objection-scripture-reliable.md`: Remove unsupported claims that transmission is simply solved.
  Separate preservation, composition, canon, genre, historical reliability, and inspiration, with a clear handoff to the Gospel discussion.
- `which-god\which-god-objection-projection.md`: Distinguish the origin of a belief from its truth, while acknowledging the relevance of psychological explanations to particular evidence.

**Observed examples:** personality lines 23-37; revelation lines 27-34; Israel lines 96-108; Scripture reliability lines 15-29.

**Completion criteria:** The transition to Jesus is an explicit move into historical inquiry, not a deduction that Christianity follows immediately from classical theism.

### 5.3 Why Jesus? - sources and calibrated historical inference

**Why:** The reader must be able to inspect historical premises without first accepting biblical inspiration or ecclesial authority.

- `why-jesus\why-jesus.md`: Add a short direct-entry orientation. State the question and relevant preceding premises without assuming the visitor read earlier stages.
- `why-jesus\jesus-who-was-he.md`: Provide locatable references for named ancient sources and explain what each establishes.
  Keep Incarnation fittingness distinct from evidence that the Incarnation occurred.
- `why-jesus\jesus-gospel-reliability.md`: Highest-priority historical revision. Distinguish textual preservation from historical accuracy.
  Explain dating, dependence, genre, testimony, and limitations. Qualify categorical claims about legends and invented details.
- `why-jesus\jesus-divine-claims.md`: Distinguish Gospel reports, historical judgments, interpretation, and the divine-identity conclusion.
  Avoid making the next inference depend on more historical certainty than the sources article establishes.
- `why-jesus\jesus-trinity.md`: Retain the substantive distinctions and rejection of modalism/tritheism. Remove or tightly limit the misleading candle analogy.
  Explain why a distinction answers a particular objection without implying it resolves every coherence question.
- `why-jesus\jesus-ot-prophecy.md`: Preserve qualifications about typology, narrative shaping, and cumulative force. Verify passages and interpretations used as evidence.
- `why-jesus\jesus-resurrection.md`: Separate the evidence for death, burial, appearances, conviction, empty tomb, and particular martyrdoms.
  Source consensus claims precisely or omit them. Distinguish divine possibility from historical probability and subsequent theological interpretation.
- `why-jesus\jesus-objections.md`: Retain the hub; offer targeted routes to disputed historical premises.
- `why-jesus\jesus-objection-did-he-exist.md`: Anchor the modest historical claim in identifiable evidence; distinguish existence from divine identity.
- `why-jesus\jesus-objection-legend.md`: Replace timing-as-disproof with a sourced account of what early evidence does and does not exclude.
- `why-jesus\jesus-objection-resurrection-alternatives.md`: Evaluate combined explanations as well as isolated theories.
  Remove assertions that rejection can only reflect unwillingness to accept the conclusion's cost. Source medical and martyrdom claims individually.
- `why-jesus\jesus-objection-pagan-parallels.md`: Compare specific alleged parallels, their sources, dates, and actual similarities instead of broad dismissal.
- `why-jesus\jesus-objection-gospel-contradictions.md`: Address examples actually raised, including time-of-crucifixion differences.
  Distinguish plausible harmonization from established resolution; incorporate literary relationships between the Gospels.
- `why-jesus\jesus-founded-church.md`: Retain the commission/community argument. State that identifying that community with Roman Catholic authority requires further steps.

**Observed examples:** Gospel reliability lines 23-42; Resurrection lines 19-46 and 54-60; alternatives lines 44-51; discrepancies lines 9-34.

**Completion criteria:** Major historical assertions have usable sources and appropriately bounded claims.
The written case remains intelligible without watching videos. Neither Aquinas nor a Church document is used as independent historical proof for an unconvinced reader.

### 5.4 Why the Church? - make the Catholic identification a positive case

**Why:** Apostolic Christianity and Roman Catholicism are connected claims, not identical conclusions.

- `why-the-church\why-the-church.md`: State the specifically Catholic question and distinguish it from the Jesus stage's Christian claims.
- `why-the-church\church-founded-by-christ.md`: Preserve the enduring visible-community argument; clarify its role relative to the preceding stage to reduce repetition.
- `why-the-church\church-recognizing-it.md`: Justify identification criteria before applying them. Show evidence rather than letting inheritance analogies carry the conclusion.
- `why-the-church\church-four-marks.md`: Distinguish Catholic interpretation of the marks from the comparative argument that identifies a communion.
  Avoid using criteria defined around the desired conclusion without defense.
- `why-the-church\church-authority.md`: Explain Scripture, Tradition, and Magisterium positively. Engage Protestant subordinate authorities and Orthodox conciliarity fairly.
- `why-the-church\church-objections.md`: Preserve the objection index and link it to the relevant positive argument, not only the stage conclusion.
- `why-the-church\church-objection-sola-scriptura.md`: Engage serious accounts of scriptural sufficiency and subordinate ecclesial authority, not merely isolated private interpretation.
- `why-the-church\church-objection-scandals.md`: Address institutional wrongdoing, accountability, and safety directly.
  Do not treat survival or the hospital analogy as sufficient evidence of divine protection.
- `why-the-church\church-objection-reformation.md`: Do not assume reform requires belief in total disappearance of the Church or complete loss of the Gospel.
- `why-the-church\church-objection-papacy.md`: Connect Peter, a continuing office, Roman succession, and the scope of authority using distinct arguments and sources.
- `why-the-church\church-objection-orthodoxy.md`: Preserve recognition of apostolic succession and valid sacraments.
  Replace sweeping claims about councils, moral drift, and missing authority with specific, sourced comparisons and serious Orthodox replies.
- `why-the-church\church-objection-mary.md` and `church-objection-saints.md`: Retain worship/veneration and intercession distinctions.
  Separate demonstrating permissibility, explaining doctrine, and establishing a particular historical practice. Do not imply these uniquely distinguish Rome from Orthodoxy.
- `why-the-church\church-objection-science-genesis.md`: Distinguish compatibility claims, interpretation, and historical conduct. Remove absolute triumphalist generalizations.
- `why-the-church\where-the-path-has-led.md`: Summarize conclusions conditionally and identify premises a reader may still dispute.
- `why-the-church\where-the-path-challenge.md`: Keep prayer as an optional invitation. Offer a simple prayer addressed to God for readers unable in conscience to pray the Rosary.
  Remove language portraying refusal as irrational; do not present prayer experience as independent verification of historical claims.

**New or substantially expanded positive treatments, in this order:**

1. **Roman authority and succession.** Prefer expanding and connecting existing articles before adding a separate page; make each inferential step inspectable.
2. **Doctrinal development.** Proposed `church-doctrinal-development.md`: distinguish growth in understanding from contradiction and explain how examples are judged.
3. **Salvation, grace, and justification.** Proposed `church-salvation-and-grace.md`: explain the Catholic claim, major Protestant concerns, faith/works, and dependence on grace.
4. **The Eucharist.** Proposed `church-eucharist.md`: present the positive scriptural, historical, and theological case, alongside serious alternative readings.

Do not invent Summa locators in advance. Verify primary passages during drafting and explain whether they provide philosophical reasoning or theological exposition.
Do not add all four as required reading before the reader may explore Coming Home.

**Observed examples:** recognition lines 18-49; papacy lines 19-44; Orthodoxy lines 27-42 and 57-60; scandals lines 21-37.

**Completion criteria:** A reader can articulate why the argument points specifically to Catholic communion and identify the supporting evidence for each additional step.

### 5.5 Coming Home - invitation without assumed assent

**Why:** Practical next steps should help people who are ready without misclassifying unresolved intellectual objections as fear or resistance.

- `coming-home\coming-home.md`: Preserve the three starting situations. Replace the claim that reasoning has simply done its work with a conditional invitation.
- `coming-home\home-not-yet-christian.md`: Distinguish interest, conviction, faith, and baptism. Keep further study available rather than announcing that study should end.
- `coming-home\home-not-yet-catholic.md`: Preserve recognition of existing Christian grace. Explain reception into communion without promising a uniform process.
- `coming-home\home-returning-catholic.md`: Retain reassurance. Distinguish a straightforward return from circumstances needing individual pastoral/canonical advice.
- `coming-home\home-what-church-offers.md`: Link sacramental benefits to the positive explanations of salvation and Eucharist instead of assuming those claims were established elsewhere.
- `coming-home\home-why-practice.md`: Explain why truth calls for lived response without treating the reader's current religious life as necessarily empty or insincere.
- `coming-home\home-obstacles.md`: Separate intellectual objections, practical difficulties, personal wounds, and readiness. Remove the claim that obstacles are rarely intellectual.
- `coming-home\home-how-to-begin.md`: Make parish contact possible without commitment. Offer a clear step for someone still exploring.
- `coming-home\begin-returning-catholic.md`: Explain how to seek Confession and conversation; avoid implying every situation is resolved by one generic instruction.
- `coming-home\begin-entering-outside.md`: Distinguish unbaptized catechumens from baptized candidates and describe parish-specific formation without guaranteeing timing.
- `coming-home\begin-eastern-orthodox.md`: Preserve sacramental recognition and Eastern Catholic possibilities; verify canonical details before revising procedural claims.

Add a short, directly linked explanation of attending Mass before reception, Communion expectations, and how to ask questions without committing.
Provide safe options for people harmed in church settings, including approaching a different parish or appropriate safeguarding support.
Do not make Discover's devotional tools conditional on finishing Inquiry or joining the Church.

**Observed examples:** Coming Home lines 10-12; obstacles lines 10-14; not-yet-Christian lines 40-45; challenge lines 16-20 and 30-35.

**Completion criteria:** Both a convinced reader and an unconvinced but interested reader receive an honest, practical next step.

## 6. Reader experience and technical changes

Paths here are relative to the repository root. Implementation should reuse existing helpers and tests before adding services or files.

### 6.1 Home and entry points

Affected: `src\Respondeo.UI\Pages\Home.razor`, `Components\Inquiry\StageReel.razor`, its scoped CSS, and `Navigation\SiteNavigation.cs`.

- Keep the question-led introduction and five self-locating prompts.
- Replace the one-question-visible reel with five readily discoverable, responsive entry choices using existing card/list components.
- Make the recommended beginning clear without making later stages look locked or illegitimate.
- Add a direct route to questions/objections and, when available, a quiet resume-reading link.
- Keep Discover and source readers accessible but visually secondary to the main Inquiry invitation.

**Why:** A Protestant or returning Catholic should not have to discover a hidden carousel position to recognize their entry point.
The present reel is already flat; this is a discoverability decision, not a promise that removing a nonexistent 3D effect fixes rendering issues.

**Acceptance:** All five choices are discoverable without horizontal interaction on a phone; keyboard order is logical; no reliance on animation or JavaScript for link access.

### 6.2 Stage flow and routes

Affected: `src\Respondeo.Content\wwwroot\inquiry\flow.json`, `Contracts\IInquiryFlow.cs`, `Contracts\InquiryContracts.cs`, and `Inquiry\InquiryFlowService.cs`.
Also affected: `src\Respondeo.UI\Pages\Stage.razor`, `Navigation\SiteNavigation.cs`, and `Navigation\ContentRoutes.cs`.

- Retain `flow.json` as the owner of the Inquiry stage sequence and branch decisions.
- Make stage UI consume declared entry/order/title information rather than assuming entry ID equals route slug in several separate places.
- Preserve the current five routes and article IDs. Add redirects only if an actual rename is unavoidable.
- Replace transition copy that assumes every objection is resolved with accurate conditional invitations.
- At major conclusions provide a forward route and a relevant route for examining an unresolved premise, where useful.
- Allow readers to move between stages through existing navigation; do not build a personalized prerequisite engine.

If distinct branch presentation is genuinely needed, add one small optional branch-purpose field after prototyping with current labels/prompts.
Do not introduce a general graph taxonomy merely for styling.

**Acceptance:** Stage order/entries agree across home, navigation, and flow; every rendered branch resolves; existing URLs and deep links retain their meaning.

### 6.3 Article depth and accordion behavior

Affected: `src\Respondeo.UI\Components\Inquiry\NodeArticle.razor`, `Components\SectionAccordion.razor`, and `Pages\Node.razor`.

- Keep the main argument understandable without opening every panel.
- Preserve shareable `?section=` links and canonical standalone article URLs.
- Display a child's review notice when its body is embedded in an accordion.
- Provide an explicit standalone link for an expanded section, where its own branches and full context are available.
- Avoid recursively nesting complete article readers and branch grids inside every accordion; reuse a small notice/body treatment if needed.
- Add nearby definitions or optional conceptual links rather than a large mandatory glossary system.

**Why:** At present the accordion renders child bodies only. A reader can miss both that child's own next steps and its review status.

**Acceptance:** Embedded and standalone presentations do not contradict each other about review status; mobile expansion does not discard the reader's location.

### 6.4 Question index and search

Affected: Inquiry contracts/services and a small Inquiry browse surface, reusing existing search and card components where appropriate.
A proposed route is `inquiry/questions`; confirm compatibility with the generic stage/node routes before choosing it.

- Start with a browsable, grouped list of existing questions and objections.
- Add local search over titles, summaries, and curated tags. Include child articles, not just top-level branch nodes.
- Use explicit helpful aliases only where ordinary wording differs from the title; do not duplicate full article bodies into new metadata.
- Label results with their stage and open canonical standalone pages.
- Include a useful no-results state and a route back to the full index.
- Add full-text indexing only if reader tasks demonstrate that metadata search is insufficient.

**Acceptance:** The canonical titles and representative ordinary-language queries for evil, hiddenness, Resurrection, papacy, and becoming Catholic yield useful destinations.
Search is usable by keyboard and screen reader and sends no religious query text to an external search service.

### 6.5 Reading continuity

Affected: `src\Respondeo.UI\Services\Navigation\VisitedNodes.cs`, `BreadcrumbTrail.cs`, `Pages\Home.razor`, and shared storage helpers.

- Separate the current breadcrumb trail from optional longer-lived reading history.
- Returning home may reset the breadcrumb, but must not silently erase a reader's chosen saved history.
- Preserve current-tab continuity without requiring an account.
- Offer an explicit local-device remember/reset choice before adding cross-session persistence; do not silently migrate session data into durable religious-interest records.
- Store only what supports the feature: article identity and necessary reading location, not belief labels or inferred acceptance.
- Handle unavailable/corrupt storage using existing application notification/logging conventions; reading must remain usable without persistence.
- Describe an opened page as visited, not read, accepted, understood, or completed.

**Acceptance:** Home, refresh, browser back, and Summa excursions preserve the appropriate context; reset works; unavailable storage does not prevent reading.

### 6.6 Inquiry-to-Summa excursions

Affected: `src\Respondeo.UI\Pages\SummaQuestion.razor`, `Services\Summa\SummaQuestionPresenter.cs`, `wwwroot\js\summa-refs.js`, and existing interop/origin helpers.

- Extend the existing origin concept to support an internal Inquiry source URL, label, and optional section/anchor.
- Preserve current Summa-to-Summa behavior and precise article/objection/reply targets.
- Provide a visible return-to-Inquiry action after a citation jump, without requiring browser back.
- Validate internal origin routes; do not accept arbitrary external return URLs.
- Explain what the cited passage contributes, preferably in the originating article; do not force a new overlay reader as the first implementation.
- Reuse the existing Summa orientation. Do not introduce a second incompatible explanation of its article structure.

**Acceptance:** A reader opens a citation from an accordion section, lands on the correct Summa passage, and returns to the original section.
Direct shared Summa URLs work without a fictitious origin; opening in another tab does not require cross-tab tracking.

### 6.7 Authoring and review integrity

Affected: `src\Respondeo.Content\Inquiry\InquiryFrontMatter.cs`, `InquiryParser.cs`, contracts, reader notices, and relevant content tests.

- Establish explicit states for draft/unreviewed, editorially reviewed, and any separately documented specialist review.
- Show a neutral unreviewed/unknown state when review is absent; do not silently grandfather all current articles into approval.
- Validate allowed values and require meaningful evidence before using a reviewed state.
- Keep article-specific bibliographies in Markdown initially. Add structured source metadata only if a concrete display/validation requirement warrants it.
- Validate manifest membership, unique IDs, entries, edge endpoints, sections, and review values at build/test time.
- Check intentional leaf pages and reachability through both branches and accordion sections; do not treat every leaf as a broken route.
- Preserve graceful runtime handling of missing assets while surfacing authoring failures clearly during validation.

**Acceptance:** Mistyped review status and duplicate identities fail authoring checks; an omitted status cannot masquerade as approval in either reader presentation.

## 7. Implementation sequence and decision gates

These are sequential work packages, not time estimates or permission to expand scope indefinitely.
Each package should leave a usable app and a reviewable diff. Keep source acquisition separate from claims of completed source verification.

### Package 1 - establish the standard with Why God?

1. Capture the argument and source requirements for the critical philosophical revisions.
2. Repair contingency, then the causal/governance bridges and divine attributes.
3. Revise evil, hiddenness, burden of proof, and stage invitation language.
4. Align the complete 17-article stage with the flexible editorial pattern.
5. Adjust only the directly related flow copy and links.

**Gate:** Review the revised stage as a skeptical reader before applying its pattern elsewhere.
Do not wait for a complete site redesign to publish careful content improvements.

### Package 2 - repair the revelation and historical bridges

1. Align Which God? with the reason/history/faith boundary.
2. Acquire and inspect sources for Gospel reliability, divine claims, and Resurrection premises.
3. Revise those central articles and their overlapping objection replies together.
4. Verify that every page's claims fit the level of support actually supplied.

**Gate:** No new consensus, dating, martyrdom, or textual-transmission assertion is introduced without a locatable supporting source and stated limits.

### Package 3 - strengthen the positive Catholic case

1. Expand the Roman-authority chain using existing pages where possible.
2. Add doctrinal-development treatment.
3. Add salvation/grace/justification and Eucharistic treatments.
4. Revise competing-view replies and the Church-stage conclusion.
5. Wire new pages into manifest, flow, source links, and index preparation.

**Gate:** The additional Catholic conclusion has its own evidence rather than borrowing certainty from the Resurrection argument.

### Package 4 - improve practical invitations

1. Remove assumed-assent language throughout Coming Home and relevant flow transitions.
2. Clarify entry situations, Mass attendance, Communion, pastoral advice, and safety.
3. Preserve a clear route for continued inquiry.
4. Connect Discover as available support, not as evidence that the reader has converted.

**Gate:** Ready, hesitant, and unconvinced readers each have a truthful next step.

### Package 5 - ship the reader-experience improvements

Implement in small slices:

1. Explicit review-state handling, including accordion notices.
2. Stage-definition consistency and visible home entry choices.
3. Question index, then lightweight search.
4. Inquiry-to-Summa return context.
5. Session continuity and optional durable local history.

**Gate:** Each slice preserves route compatibility, mobile usability, and existing source-reader behavior before the next slice begins.
Do not bundle every UI change into an all-or-nothing redesign.

### Package 6 - reader validation and final refinement

1. Conduct the tasks in section 8 with readers from different starting positions.
2. Record navigation failures separately from disagreement with the argument.
3. Correct the observed friction and unsupported claims before expanding the content catalog further.
4. Update directly related documentation and the local Graphify graph for implemented structural changes.

**Gate:** Mark a change complete only when its promised behavior or editorial outcome has been checked; do not treat passing tests as evidence of persuasion.

## 8. Verification plan

### Existing automated foundations

Reuse the relevant suites in `tests\Respondeo.UnitTests`:

- `Content\ContentLinkIntegrityTests.cs`: article links, flow/section targets, Summa references, and citation style.
- `Content\StageNavigationConsistencyTests.cs` and `StageFolderIntegrityTests.cs`: stage and navigation consistency.
- `Services\InquiryFlowServiceTests.cs`: declared flow behavior.
- Existing home, stage, node, accordion, storage, route, and Summa component/service tests where their behavior changes.

Extend only the contracts introduced by each package. Start with targeted tests, build the affected projects, and expand to the full unit suite at integration points.
Use `tests\Respondeo.AcceptanceTests` for browser/mobile interaction and navigation checks, not a new acceptance-test project.

### Additional behavioral checks

- Existing five stage URLs, canonical article URLs, `?section=` links, and Summa fragments remain valid.
- Every proposed new article is manifested and reachable through an intentional reader path.
- The home page exposes all five choices without horizontal discovery.
- Stage order and entry metadata agree with rendered destinations.
- Missing/invalid review status cannot imply approval; child notices match standalone notices.
- Search includes objection children and provides useful results for specified representative queries.
- Reading history survives only as promised, can be cleared, and is never represented as assent.
- Source excursions return to the correct article/section; existing Summa cross-reference returns still work.
- Keyboard, focus, back/forward, reduced-motion, narrow-screen, and Android WebView behavior are checked where affected.
- Offline behavior remains consistent with the application's existing content-pack guarantees; do not promise an uncached external source works offline.

### Editorial checks

For each major revised argument, a reviewer should be able to answer:

1. What exactly is the conclusion?
2. Which premises are asserted, argued, or accepted from a source?
3. Does the conclusion follow at the stated level of confidence?
4. Is the strongest relevant objection fairly represented?
5. Does each citation support the claim made beside it?
6. Are historical evidence, doctrinal authority, and philosophical reasoning distinguished?
7. Can a reader locate the unresolved issue without being told their disagreement is insincere?

Do not encode these judgments as keyword tests that reward the appearance of rigor without assessing the argument.
Use qualified philosophical, historical, theological, and pastoral review where appropriate; identify what was and was not reviewed.

### Reader tasks

Recruit, where feasible, readers representing skeptical/agnostic, non-Catholic Christian, returning Catholic, and new-Catholic starting positions.
Participation should be voluntary; collect task feedback, not sensitive belief profiles.

Ask readers to:

- Find a serious objection they hold without knowing its exact article title.
- Explain a selected argument's conclusion and identify a premise they would dispute.
- Examine an original Summa passage and return to the explanation.
- Find a next step when they remain unconvinced.
- Find how to attend Mass or approach a parish without already committing to conversion.

For the proposed home layout, all five entries must be discoverable without carousel interaction.
For the source-return task, the final location must be the originating article and relevant section, not merely the stage landing page.
For editorial comprehension, ask for a paraphrase rather than a yes/no claim of understanding.
Record failures and reasons; do not invent conversion-rate targets or regard agreement as the usability pass condition.

## 9. Risks and controls

| Risk | Control |
|---|---|
| An enormous rewrite delays every useful improvement | Publish one coherent package at a time; first complete Why God? |
| Qualification drains the case of conviction | State conclusions directly and show their warrants; remove overclaiming, not the invitation |
| New Summa quotations become decoration | Explain each passage's actual role; use historical sources for historical claims |
| A content schema grows into a rules framework | Begin with Markdown, current flow, and minimal review/branch metadata |
| A unified stage source becomes a broad navigation rewrite | Consolidate Inquiry metadata only; preserve existing route behavior |
| Saved history reveals sensitive interests | Session continuity first; explicit local persistence choice; reset; no belief inference or telemetry |
| A visual redesign reintroduces Android navigation flicker | Small slices, existing scroll conventions, mobile acceptance checks, no new animation requirement |
| Readers mistake publication for approval | Honest explicit review state and no invented reviewer/authority claims |
| Fairness changes become doctrinal ambiguity | Distinguish presenting an opposing argument accurately from endorsing it |
| Practical advice overpromises a canonical outcome | Verify procedural claims and direct complex cases to appropriate individual guidance |

## 10. Definition of completion

This renewal is complete when:

- The existing five-stage mission remains recognizable and its routes remain usable.
- Critical philosophical and historical inferences have been revised with appropriate support and limits.
- The specifically Catholic case includes developed positive treatments, not just objection replies.
- Readers can examine evidence, retain context, and continue honestly whether persuaded or not.
- Review status is explicit and does not imply a review that never happened.
- Ready readers have sound practical guidance; other readers can keep exploring without being treated as completed converts.
- Relevant builds, targeted/full tests, acceptance tasks, and editorial checks pass or have clearly recorded unresolved limits.
- Discover, the calendar, Hours, Bible, and Summa remain available without being expanded or rewritten unnecessarily.

## 11. Recommended first implementation

Begin with Package 1, not a new home-page design or a database migration.
Within it, start with `why-god\five-ways-contingency.md`, then revise the connected explanations and objection language as one coherent stage.

Use that revised stage to agree on the standard before scaling it across the remaining content.
Then address the Gospel/Resurrection and Roman-authority bridges in the order above.

The central change is not more features. It is a clearer and better-supported path from a real question to a freely considered next step.

## 12. Implementation and verification record

### Delivered for user review

- Revised all 69 original articles across the five existing stages, retaining their IDs and routes.
  The revisions distinguish philosophical inference, historical judgment, Catholic teaching, and practical invitation instead of presenting every transition as a deduction.
- Added three optional positive Catholic treatments: doctrinal development, salvation and grace, and the Eucharist. The manifest now contains 72 articles.
  New choices connect these treatments from the authority and objection hubs without imposing prerequisites or removing existing onward routes.
- Initially replaced the home carousel with a grid; user review rejected that visual change.
  Restored the floating-question reel with native horizontal scrolling, previous/next controls, keyboard navigation, and reduced-motion-aware scrolling.
  Compact stage links keep all five starting points discoverable without requiring a swipe. Reading preferences are collapsed; resume links and storage warnings remain visible.
  Home and stage pages use declared flow entries rather than assuming a stage's URL identifies its entry article.
- Added a grouped question index at `/inquiry/questions`, including standalone links for accordion children.
  Local search covers titles, summaries, tags, and curated question aliases; it is not full-body search or an external search service.
  Registered the index with the sitemap/static-entry generator and added published deep-link checks for it and all three new articles.
- Added typed unreviewed, editorially reviewed, and specialist-reviewed states.
  Missing status now means unreviewed; reviewed states require a named reviewer, an exact ISO date, and a review scope.
  Article and accordion notices share the same presentation and never imply Church approval.
- Added optional last-question continuity. Session storage is the default; durable device storage requires an explicit choice and can be reset.
  It records the last opened article's identity, not assent, belief, completion, or an account profile.
  Resume is article-level, not an exact saved reading position. Denied storage retains usable in-memory navigation and exposes an honest warning.
- Added a validated return route from Summa citations to the originating Inquiry article and accordion section.
  Direct Summa visits do not acquire a fabricated origin; existing Summa browsing and browser-back behavior remain covered.
- Added authoring contracts for manifest completeness, unique IDs and paths, flow ownership and endpoints, duplicate edge-source keys, navigation agreement, and article reachability.
  These checks establish structural consistency, not the truth or persuasiveness of an argument.
- Updated the Inquiry authoring guide and acceptance-test documentation. Markdown content, flow orchestration, and reader UI remain separate; no CMS, database, or generic argument engine was added.

### Editorial evidence and authority

The philosophy/revelation revision inspected Aquinas, relevant Stanford Encyclopedia of Philosophy material, Scripture, Vatican II documents, and historical or comparative sources.
The Catholic/practical revision inspected the Catechism, Vatican II, canon law, early Christian witnesses, Aquinas, and relevant safeguarding and ecumenical documents.
The Jesus/history revision inspected Tacitus (*Annals* 15.44), Josephus (*Antiquities* 20.9.1), Pliny (*Letters* 10.96-97), Scripture, Catechism 232-247 and 253-255,
and relevant texts from Plutarch, Euripides, and Ovid.
It distinguishes Resurrection evidence strands and combined alternatives, qualifies Gospel dating and dependence, and leaves the Mark-John crucifixion-time tension unresolved.
These source checks support editorial corrections; they do not replace qualified review of contested premises, historical inferences, doctrine, canon law, or safeguarding advice.

All revised and new articles remain without recorded independent approval.
AI-assisted revision must not be relabeled as specialist review. The proposed human agreement on a sample and subsequent representative-reader study have not taken place.

### Verified engineering results before the reel presentation follow-up

- Full unit suite: **2,154 passed, 0 failed, 0 skipped**.
  Command: `dotnet test tests\Respondeo.UnitTests\Respondeo.UnitTests.csproj --no-restore --verbosity quiet`.
- WebAssembly build through Visual Studio, Release publish, and sitemap-generator build: successful.
- Final acceptance-project build: successful, with zero warnings and zero errors.
- Combined real-browser regression run against published Release assets: **61 passed, 0 failed, 0 skipped**.
  Covered home choices, keyboard access, phone/tablet/desktop layouts, reduced motion, enlarged Inquiry entries, and question search/reset.
  Also verified all 72 index results, history consent/reset, and denied storage.
  Navigation checks covered Inquiry sections, stage navigation, breadcrumbs, forward scroll placement, Summa browse restoration, and Inquiry-to-Summa return navigation.
  The 27 HTTP/canonical cases include the question index, all three new articles, existing deep links, and unknown-route 404 responses.
- Verified that all 72 published Inquiry article hashes match the final source files.
- All 72 authored Inquiry articles have lines under 200 characters. Authored-content and handwritten engineering whitespace checks passed.
  Generator-created feature files retain their generator's formatting.

The browser suite ran with `RESPONDEO_BASE_URL=http://127.0.0.1:5187` and `dotnet test --no-build` against `tests\Respondeo.AcceptanceTests\Respondeo.AcceptanceTests.csproj`.
The selected classes were `InquiryJourneyTests`, `HomePageFeature`, `SummaQuestionReadingFeature`, `SummaBrowseStateFeature`, `SectionsFeature`,
`BreadcrumbFeature`, `StageNavigationFeature`, `ScrollingFeature`, and `DeepLinksFeature`.
The final result is retained in the ignored `TestResults\inquiry-renewal-published-browser.trx` file under the acceptance project.

An earlier development-host run passed 34 cases but skipped 23 deployment-only checks; the published run covers those checks plus four new route cases without skips.
An initial parallel published run encountered two connection refusals with the local Python fixture, whose connection backlog is five.
The successful final run used the supported runner setting `-- xUnit.ParallelizeTestCollections=false`; no application assertions or HTTP checks were relaxed.
The static host used the repository's `.github\actions\acceptance-tests\serve.py` resolver against generated per-route entry pages.

### Reel presentation follow-up requested during user review

The restored reel keeps the improved content and question-index link, presents one floating question at a time, and exposes compact links to all five stages.
It uses native horizontal scroll snapping instead of transformed slides, leaves vertical scrolling to the browser, and avoids automatic rotation.
Previous/next controls, arrow keys, Home/End, and reduced-motion-aware scrolling provide alternative ways to explore.
Reading preferences now start collapsed, while the last-question resume link and any storage warning remain visible.

- UI, WebAssembly, and acceptance-project builds succeeded; the final command-line builds reported zero warnings and zero errors.
- Targeted component/storage regressions: **23 passed, 0 failed**.
- Real-browser follow-up: **20 passed, 0 failed, 0 skipped**, covering Inquiry journeys, Home, and Summa browse restoration.
  This includes phone/tablet/desktop widths, 200% Inquiry text, reduced motion, actual browser touch input, keyboard/control navigation, and collapsed preferences with consent/reset.
- The follow-up ran against a fresh development host using `--no-build` and `-- xUnit.ParallelizeTestCollections=false`.
  Its report is retained in the acceptance project's ignored `TestResults\reel-restoration.trx` file.
- Native physical-device testing remains pending. These results do not prove Android app-switch flicker is resolved.

### Remaining review and known limits

1. Obtain qualified philosophical, historical, theological, canonical, and safeguarding review where relevant, then record its actual scope and reviewer.
2. Ask representative readers to test whether the arguments and optional next steps are understandable without assumed assent.
   Automated tests do not establish accessibility conformance, persuasive success, or freedom from reader confusion.
3. Verify the revised UI on a physical Android device and its WebView. Desktop browser viewport emulation does not prove native flicker or app-switch behavior is fixed.
4. Address the existing site-wide masthead/pillar-navigation overflow at 200% root text size separately.
   The Inquiry question and stage links fit at enlarged text; the unchanged global header still overflows on a narrow viewport. No site-wide large-text pass is claimed.
5. Review this working-tree implementation before committing or publishing. Existing calendar/Hours work and its authority limitations remain outside this change.

### Navigation graph maintenance

The local Graphify code graph was structurally refreshed after the reel follow-up: 8,177 nodes and 18,335 edges.
Documentation and article semantic extraction was not refreshed; the graph is not a completed semantic review of these revisions.
The extractor also reported 100 code files with no recovered symbols. The bounded HTML export is an aggregated community view, not a complete file-level visualization.
Existing graph diagnostics identified self-loops and same-endpoint edge-collapse risks; these are limitations of the navigation aid, not application correctness results.
