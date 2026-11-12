# Liturgy of the Hours data

## Authoring validation

All embedded Office JSON uses the same strict reader. Comments, trailing commas, case-insensitive property names, and named enum aliases remain supported.
Duplicate properties (including case variants), unknown properties, numeric/unknown enum values, and missing required constructor fields are rejected.
Optional seasonal override fields may be omitted. Parsing errors identify the resource and preserve the original JSON exception.

On first Office-data access, a shared validation gate loads all resources and checks ordinary-text references, including missing targets and cycles,
Hour template/seasonal references, Marian antiphon text keys, Common targets, and binding identity syntax.
Known breviary tokens such as `{{V}}` are expanded before checking `{text-key}` references; unknown brace tokens and null text lines fail validation.
Calendar-binding source identities are checked separately by integration tests against the union of the supported fixed and computed catalogs,
not against sampled daily winners. Calendar does not acquire a dependency on Hours.
A binding target need not have a bundled celebration record: intentionally unavailable propers and official-provider content remain supported.
These checks validate authoring and references, not liturgical authority or the completeness of protected proper texts.

## temporal-psalmody.json

Citation-only assignments for the Paschal Triduum and the Lenten Sunday Evening Prayer canticle live here rather than in the composer.
`triduum` keys are offsets from Easter Sunday: -3, -2, -1, and 0. Each entry supplies `morning` and `evening` citation lists,
except Holy Thursday morning, which must remain absent because it uses the Holy Week psalter.
The Easter-octave rule reuses the offset 0 assignment. `lentenSundayEveningCanticle` replaces only the final canticle at Sunday Evening Prayer II.

The loader validates the complete offset set, required hours, three nonblank citations per assignment, and the canticle's position,
then publishes frozen/read-only snapshots. Errors identify the resource and the affected offset and hour.
Date arithmetic, seasonal applicability, First Vespers, precedence, and participation-dependent omissions remain typed C# rules.
This extraction preserves the existing assignments and provider keys; it is not new approved-book verification or a generic rules language.
Only references are stored here, not protected prayer translations.
The [authority register](../../../docs/liturgical-authority-baseline.json), review `HOUR-02`, tracks these assignments and their pending approved-book check.

## hours.json

Hour templates provide the ordinary openings. Lauds' `introText` is the alternative introductory verse when the Invitatory does not precede it.
`seasons` may override `text`, `introText`, and `explanation`; unspecified fields retain their base values.
The opening includes the fixed `alleluia` text except in Lent and the pre-Easter Triduum (GILH 41, 85; Universal Norms 28).
Calendar's morning/evening season controls these overrides, so Easter Sunday is not mistaken for the pre-Easter Triduum.
This changes only the opening, not protected proper antiphons or responsories.

Official-provider content is preferred by default after element assignment; `Compose(..., preferOfficialText: false)` bypasses it without changing the templates or references.
Fixed bodies, alternative openings, Compline elements, and options can use scoped provider keys documented in the library README.
The sample Invitatory antiphon is stored only in `SampleContent`, not `hours.json`; disabling provider content cannot leak that dummy antiphon into a fallback Office.
Sample and non-sample provider contributions have distinct provenance flags and acknowledgments, including nested options.

`participationOmissions` provides original attendance labels, explanations, and GILH locators for the four supported omission conditions (nn. 209, 211, 215).
Keys and each rule's `participation` must identify the same `LiturgicalParticipation` value. Date/Hour applicability is selected by the engine, not by these presentation strings.
The caller must explicitly supply participation; the default composes the Hour normally. The Christmas condition is the solemn Office of Readings vigil, not Midnight Mass alone.
Omitted Offices have no prayer steps, retain their identity and sources, and are not marked completed by the player. These notices do not certify the remaining proper texts.

## sanctoral.json

Short-reading citations for Morning and Evening Prayer on celebrations of the saints.
Citations were scraped from [liturgies.net](https://www.liturgies.net/Liturgies/Catholic/loh/commons.htm)
(commons pages and the `/saints/{slug}/` pages that have hour pages).

- `calendarBindings`: authored Calendar keys mapped to established Hours content IDs, shared by reference assignment and provider lookup.
  There are 254 migrated identities: 30 compatibility bindings and 224 direct content IDs. No existing provider key changes.
  The five transferable solemnities retain the same binding on nominal and transferred dates; the US prayer day also has an explicit binding.
  All bundled US celebrations and General fixed entries have authored identities. New direct IDs preserve existing Hours keys without redundant aliases.
  A renamed celebration retains its binding; saint-profile IDs are unrelated. Keys without a compatibility binding are already content IDs, not requests for name fallback.
  Only calendar entries without `CalendarKey` retain legacy display-name slugification; no fixed catalog entries remain unmigrated.
  Adding a binding does not supply missing proper readings or texts, change psalmody rules, or certify any assignment.
- `commons`: the readings of each Common.
- `celebrations`: keyed by the established Hours content ID (originally the slugified calendar name).
  Each entry gives its own `morning`/`evening` citations, a `common`, or both.
  A celebration with no entry, or no citation for an hour, falls back to the official source link.
- `eveningPsalmody`: explicit proper psalms and canticle on a celebration, including memorials.
  A Common's list is used only on feasts and solemnities without an explicit proper list.
  These lists do not apply when the evening is First Vespers of a Sunday or solemnity.
- `morningPsalmody`: explicit proper psalms and canticle on a celebration, including memorials with their own psalmody.
  Without an override, feasts and solemnities use Sunday, Week I; memorials keep the weekday psalter, not their Common's psalmody.
  Overrides apply only when the calendar actually celebrates the memorial, not when it is displaced by Sunday.
- `firstEveningCanticle`: a specifically prescribed First Vespers canticle, selected only when Calendar assigns that celebration's First Vespers.
  Epiphany and the Transfiguration use 1 Timothy 3:16 (GILH 137 identifies the book; exact verse limits still need approved-proper review).
  This narrow override does not validate the existing provisional First Vespers psalms or reuse `eveningPsalmody` as a First Vespers set.
  Antiphons retain `proper/{celebration}/evening-i/psalm-antiphon-3`; ordinary evening antiphons cannot fill that slot.

### Universal additions and content keys

Teresa of Calcutta (September 5) and John Henry Newman (October 9) now come from the General calendar, inherited by the US calendar.
The [decree register](../../../docs/liturgical-calendar-amendments.json) records the source evidence and permitted Common alternatives; full Office review remains pending.
Teresa retains Virgins and Newman retains Pastors as permitted defaults; neither default prescribes a memorial psalmody override.
Newman's key is now `saint-john-henry-newman-priest-and-doctor-of-the-church`, matching his current title.
Custom content using `saint-john-henry-newman-cardinal` in celebration or `proper/...` keys must migrate to the new key; the repository has no such custom bodies to migrate.
The calendar uses current rules going forward; historical title/observance reconstruction is not supported.

### Verified Saint Martin exception

[GILH 235(a)](https://www.liturgyoffice.org.uk/Resources/Rites/GILH.pdf) prescribes weekday psalms **and antiphons** for memorials unless proper ones are indicated.
Saint Martin has prescribed antiphons at both hours. His proper sets are:

- Morning: Psalm 63:2-9; Daniel 3:57-88, 56; Psalm 149; short reading Hebrews 13:7-9a (per the user's iBreviary reference).
- Evening: Psalm 15; Psalm 112; Revelation 15:3-4; short reading 1 Peter 5:1-4.

Psalmody references were cross-checked against [Universalis Morning Prayer](https://universalis.com/20261111/lauds.htm),
[Evening Prayer](https://universalis.com/20261111/vespers.htm), and liturgies.net's
[Morning](https://www.liturgies.net/saints/martin/morningprayer.htm) and [Evening](https://www.liturgies.net/saints/martin/eveningprayer.htm) offices.
Tests cover 2000-2399 in the General Roman and US calendars, including Sunday suppression and Saturday evening concurrence.
This verifies this exception, not the completeness of the entire sanctoral dataset.

### Liturgical passage arrangements

`ordinary-texts.json` supplies `psalmodyPassages` when a citation alone does not reproduce the liturgical text.
Each passage contains ordered Scripture excerpts or fixed public-domain text and a translation label.
The arrangement is the complete body: the loader does not append another Glory Be.
For Daniel 3:57-88, 56, verse 88's second portion is omitted, the ancient Latin Trinitarian blessing is inserted, and verse 56 concludes the canticle.
The English explanation of that blessing is explicitly unofficial; it is not represented as the approved English translation.
A complete licensed body, if supplied with the matching citation, replaces this fallback without alteration.

`readingPassages` separately binds exact short-reading citations to arrangements. Hebrews 13:7-9a retains verses 7-8 and only the first sentence of verse 9.
The last sentence is copied from the bundled public-domain Douay-Rheims Hebrews 13:9, not from a modern liturgical translation.
This prevents the Bible adapter's whole-verse interpretation of `9a` from including the remainder. No doxology is appended to a reading.
If a required excerpt is unavailable, the player directs the reader to an official source rather than displaying a truncated reading or using the whole-verse fallback.
The existing citation and proper/Common assignments are unchanged. The secondary Martin page currently gives 13:7-8; approved-book reconciliation of 13:7-9a remains open.
Tuesday Compline's 1 Peter 5:8-9a similarly uses verse 8 plus the opening clause of 9, ending after *strong in faith* (colon changed to a closing period).
Friday's Jeremiah 14:9a uses the closing plea of Douay-Rheims 14:9, beginning *But thou o Lord art in us*, not that verse's opening question.
These translation-specific boundaries follow the reading bodies on the secondary
[Tuesday](https://www.liturgies.net/Liturgies/Catholic/loh/compline/tuesday.htm) and
[Friday](https://www.liturgies.net/Liturgies/Catholic/loh/compline/friday.htm) pages, reviewed November 11, 2026; no modern translation was copied.
Tuesday's page labels the reading 5:8-9, but its body stops after the opening clause of 9; the existing 5:8-9a reference is retained.
Friday's published *9a* label is retained even though its corresponding Douay-Rheims words occur at the end of the verse. Do not infer this mapping from the suffix.
Both arrangements follow the selected Compline reading cycle, not a Sunday-psalm substitution. Approved-book verification remains pending.

Week III Wednesday Lauds' Job 1:21; 2:10b uses the full bundled 1:21 and only the question about receiving good and evil from 2:10, with initial capitalization.
It omits Job's rebuke and the narrator's closing comment, matching the excerpt boundary on the secondary
[Week III Wednesday page](https://www.liturgies.net/Liturgies/Catholic/loh/week3wednesdaymp.htm), reviewed November 11, 2026.
The existing citation and assignment are unchanged; approved-book verification remains pending. The assembly contains only bundled public-domain wording, not the site's modern translation.
Week IV Saturday Lauds' 2 Peter 3:13-15a retains full verses 13-14 and only the opening clause of 15, ending after *salvation*.
The clause uses bundled Douay-Rheims wording, with initial capitalization and a closing period; it omits the continuation about Paul's letters.
The secondary [Week IV Saturday page](https://www.liturgies.net/Liturgies/Catholic/loh/week4saturdaymp.htm), reviewed November 11, 2026, corroborates the boundary.
The existing citation and assignment are unchanged; approved-book verification remains pending.

The secondary [Week IV Sunday page](https://www.liturgies.net/Liturgies/Catholic/loh/week4sundaymp.htm) labels 2 Timothy 2:8a, 11-13 but prints all of verse 8.
That label/body disagreement does not establish a safe 8a cutoff. No Timothy arrangement or assignment change is made; approved-book verification is required.
Other a/b portions still require individual review; this is not a general linguistic verse-splitting algorithm.

Week III Sunday Lauds' Ezekiel 37:12b-14 begins with "Thus saith our Lord God", omitting the instruction to prophesy and speak from the start of bundled verse 12.
The retained clause is copied exactly from bundled Douay-Rheims (`ezechiel.json`); verses 13-14 are loaded normally. No punctuation or wording is modernized.
The secondary [Week III Sunday page](https://www.liturgies.net/Liturgies/Catholic/loh/week3sundaymp.htm), reviewed 2026-11-11, corroborates the proclamation boundary.
No protected text is imported. The existing citation/assignment is retained, and approved-book verification remains pending.

Week II Wednesday Vespers' 1 Peter 5:5b-7 begins with *And do ye all insinuate humility*, excluding the preceding instruction to young men.
The opening is exact bundled Douay-Rheims wording, followed by full verses 6-7; no wording or punctuation is modernized.
The secondary [Week II Wednesday page](https://www.liturgies.net/Liturgies/Catholic/loh/week2wednesdayep.htm), reviewed 2026-11-11, corroborates the opening but gives 5:5b-11.
That longer ending conflicts with the retained 5:5b-7 assignment. Only the opening boundary is addressed here; approved-book reconciliation of the ending remains required.
No protected text is imported, and the citation/Calendar assignment is unchanged.
The same review found longer readings on the secondary Week I Monday (Colossians 1:9b-13 versus 1:9b-11) and Tuesday (1 John 3:1-3 versus 3:1a, 2) pages:
[Monday](https://www.liturgies.net/Liturgies/Catholic/loh/week1mondayep.htm) and [Tuesday](https://www.liturgies.net/Liturgies/Catholic/loh/week1tuesdayep.htm).
Neither assignment is changed. Colossians' supported opening is now arranged below; 1 John's conflicting excerpt remains unavailable pending approved-book review.

Week II Friday Vespers' 1 Corinthians 2:7-10a retains full verses 7-9 and only the first sentence of verse 10, ending at *by his Spirit*.
The sentence is copied exactly from bundled Douay-Rheims; it omits the continuation about the Spirit searching all things. No wording or punctuation is modernized.
The secondary [Week II Friday page](https://www.liturgies.net/Liturgies/Catholic/loh/week2fridayep.htm), reviewed 2026-11-11, corroborates the ending but starts at verse 6.
That 2:6-10a versus 2:7-10a assignment conflict is recorded, not silently resolved. The existing start and Calendar assignment remain unchanged pending approved-book reconciliation.
No protected text is imported, and this boundary evidence does not certify the complete assignment.

Week I Monday Vespers' Colossians 1:9b-11 now starts at *That you may be filled*, followed by full bundled verses 10-11.
The opening uses bundled Douay-Rheims wording with initial capitalization only. The previously reviewed Monday page supports that opening, not the retained verse 11 ending.
The secondary source's 1:9b-13 assignment still conflicts with the retained citation; the ending remains pending approved-book reconciliation.

### Partial-reading fallback inventory (2026-11-11)

All 29 distinct a/b/c short-reading citations in the embedded Psalter, Commons, and celebration propers are classified by regression tests:
9 retain explicit arrangements above; the remaining 20 permit whole-verse Bible resolution while preserving their assigned references, including suffixes.
Citation-bearing arrangement parts also permit whole boundary verses. Inline supplied bodies retain precedence and no reading receives an added doxology.
Exact clause editing is deferred, not a release blocker. This does not certify assignments or resolve the approved-book discrepancies recorded above.
The following unarranged citations use whole-verse DRB fallback when no inline body is supplied:

| Scope | Whole-verse fallback references |
| --- | --- |
| Weekday Lauds | 2 Timothy 2:8a, 11-13 |
| Weekday Vespers | 1 John 3:1a, 2; Colossians 1:2b-6a |
| Common of Virgins | 1 Corinthians 7:32b, 34a |
| Celebration propers | 1 Corinthians 15:1-2a, 3-4; 1 Corinthians 15:3-5, 8a; 2 Corinthians 6:16b; 7:1; Acts 26:16b-18; Acts 5:12a, 14-16; Acts 6:2b-5a |
| Celebration propers (continued) | Colossians 1:3-6a; Ephesians 5:25b-27; Genesis 28:12-13a; Hebrews 2:9b-10; Isaiah 61:1-2a; Joel 2:27-3:1a; Micah 5:2-4a |
| Celebration propers (continued) | Revelation 1:4b-5; Romans 5:20b-21; Romans 8:3b-4 |

Timothy/1 John have the secondary conflicts recorded above; the Saturday Vespers URL redirects to Morning Prayer and is not evidence for a Colossians boundary.
The remaining proper/Common excerpts have not been individually verified against approved books. Do not invent cutoffs from punctuation or a suffix alone.
Whole-verse readings, Calendar selection, Compline schema choices, and licensed-body precedence are unchanged. Exact clause extraction is no longer required for fallback.

### Partial-psalmody fallback (2026-11-11)

The embedded Psalter/Common/proper psalmody contains five distinct a/b/c citations without reviewed arrangements:
Judith 16:2-3a, 13-15; Exodus 15:1-4a, 8-13, 17-18; Habakkuk 3:2-4, 13a, 15-19; Isaiah 66:10-14a; Revelation 11:17-18; 12:10b-12a.
These permit whole-verse DRB fallback with the original references and existing doxology behavior. A suffix alone does not require an official-source notice.
The shared loader applies this policy to direct steps, options, stanzas, and arrangement parts. Explicit arrangements and inline bodies retain precedence.
Required stanza sets remain all-or-nothing, with no full-parent-psalm fallback. Missing Bible text/arrangements still show a source notice; Daniel's assembly is unchanged.
Tests retain the five-citation inventory and forward 2026-11-11 through 2028 Office selection in all profiles, including temporal/proper reuse.
This adds no textual boundaries or source authority; assignment, numbering, and prescribed-ending review remain distinct from deferred clause precision.

### Outstanding questions

1. **Apostles, evening reading.** The Common of Apostles page gives Ephesians 4:13.
   The Saint Andrew page gives Ephesians 4:11-13. The data uses 4:11-13; confirm against the printed breviary.
2. **Common assignments for memorials.** liturgies.net does not state which Common each memorial uses,
   so each was assigned from the celebration's title. A liturgist should review the list, in particular:
   - Bishops or popes who were also martyrs (filed under Martyrs, not Pastors).
   - Doctors who were also bishops (filed under Doctors, not Pastors).
   - Religious, abbots, hermits and lay saints (filed under Holy Men or Holy Women by first name).
   - Saints Martha, Mary and Lazarus (filed under Holy Women).
   - Saint Juan Diego Cuauhtlatoatzin and Saint Isidore (filed under Holy Men).
   - Our Lady memorials (filed under the Blessed Virgin Mary).
   - The Dedication of Saint Mary Major and of the Basilicas of Saints Peter and Paul
     (filed under Dedication of a Church).
3. **Memorials with their own readings, not yet sourced.** These have no entry and fall back to the official link:
   - The Most Holy Name of Jesus
   - Saint Joseph the Worker
   - The Passion of Saint John the Baptist
   - Independence Day (US)
4. **Feasts and solemnities with their own readings that liturgies.net does not publish.** These include the
   Presentation of the Lord, the Annunciation, the Visitation, the Nativity of John the Baptist,
   Saints Peter and Paul, the Transfiguration, the Assumption, the Nativity of Mary, the Exaltation of the Cross,
   the Dedication of the Lateran, the Immaculate Conception and Our Lady of Guadalupe (US feast).
   They now carry proper or Common readings and psalmody (see item 8); spot-check them against the printed breviary.
5. **Missing evening readings.**
   - Saint Stephen and the Holy Innocents: Evening Prayer is of the Christmas Octave; confirm no saint's reading is wanted.
   - Saint Martin's Evening Prayer reading has now been verified as 1 Peter 5:1-4.
6. **Holy Men vs. Holy Women.** Both Commons currently carry the same citations (Romans 12:1-2 / Romans 8:28-30).
   Confirm that the breviary indeed does not differ, or that a sub-common (religious, works of mercy, educators) is not intended.
7. **Optional memorials during privileged seasons** (Lent, late Advent) are commemorations only;
   confirm the short reading should then stay with the weekday rather than the Common.
8. **Festal psalmody and readings.**
   - Every fixed feast and solemnity now has proper or Common short readings and Evening Prayer psalmody;
     festal Morning Prayer uses the Sunday Week I psalmody.
   - The Passion of Saint John the Baptist and the Lateran Dedication take their Commons.
   - Moveable feasts and solemnities (Epiphany, Baptism, Holy Family, Ascension, Pentecost, Trinity, Corpus Christi,
     Sacred Heart, Christ the King) now carry proper readings and psalmody; the moveable Marian memorials take the Common.
   - Unit tests check that every citation parses, every referenced Common exists, and every psalmody has three entries.
   - All festal citations still need a spot-check against the printed breviary.
