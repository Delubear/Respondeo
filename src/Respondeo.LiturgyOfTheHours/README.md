# Respondeo.LiturgyOfTheHours

Builds the step-by-step order of the Liturgy of the Hours (Ordinary Form) used by the Discover > Hours pages.

## Scope

- **Hours covered:** Morning Prayer (Lauds), Evening Prayer (Vespers), and Night Prayer (Compline).
- **Not covered (yet):** Office of Readings, Daytime Prayer (Terce, Sext, None), and the Invitatory as a separate hour.
- The site is public, free, and ad-free. Being non-commercial does **not** by itself grant permission to reproduce copyrighted liturgical texts.

### Accuracy boundary

The engine is not certified complete for every year, region, or occasion. The General Roman and US calendar options are not diocesan or religious-order calendars.
The current dataset still needs a complete review against the applicable approved edition, including other memorial propers,
seasonal exceptions, First Vespers psalmody, commemorations, and regional changes. See [the data review questions](Data/README.md).
Future calendar or edition changes require reviewed data updates; a Gregorian date range is not a permanent liturgical guarantee.

Regression tests establish specific rules and consistency, not independent liturgical authority.
`HoursContentFileTests` composes all supported hours for 2020-2040 in both regions against the selected `HOURS_CONTENT_DIR`;
this rejects mismatched full psalmody bodies but does not prove that all prescribed content exists or that its wording is approved.
Local sample-content validation cannot certify the separate licensed release dataset.
Missing prescribed psalm antiphons are explicitly identified in the player with a link to the full office.
Public-domain Scripture and unofficial translations must not be mistaken for an approved liturgical edition.

### Participation-dependent omissions

`ILiturgyOfTheHours.Compose` accepts optional `participation: LiturgicalParticipation`, defaulting to `None` without assuming attendance.
For the supplied service date, GILH 209, 211, and 215 prescribe these omissions:

| Participation | Hour not said separately |
| --- | --- |
| Holy Thursday evening Mass of the Lord's Supper | Holy Thursday Vespers |
| Good Friday celebration of the Lord's Passion | Good Friday Vespers |
| Easter Vigil | Holy Saturday Compline |
| Solemn Office of Readings vigil before the Midnight Mass of Christmas | Christmas Eve Compline |

Christmas Midnight Mass attendance alone is not the last condition. Other dates and Hours are not omitted by these participation values.
`Office.ConditionalOmission` exposes the applicable condition and source locator even before attendance is supplied.
When it is confirmed, `IsOmitted` is true and `Steps` is empty; date, evening ownership, title, background week, and source links are preserved.
The shared player offers an unchecked attendance control only when relevant, explains the omission, and hides progress/completion rather than claiming the Hour was prayed.
Clearing the choice restores the Hour from its first step. Attendance is not persisted or carried across route/date/calendar changes.
This implements the omission rules only, not the Office of Readings vigil itself or a certification of all Triduum/Christmas propers.

### Service date

The shared player initially selects the local current date and lets the reader explicitly choose the day whose Office is being prayed.
GILH 84 places Compline before bed even after midnight; selecting the preceding service date preserves that night's Office and participation rule.
The selected date remains stable across midnight, calendar preference updates, and same-component Hour navigation; a fresh player defaults to today.
Changing it reloads the Office from the first step, clears attendance, and rejects invalid input with an error without replacing the existing Office.
Older asynchronous passage loads cannot overwrite the newly selected Office. Browsing another date does not advance today's persisted Invitatory cycle.
Neither the service date nor attendance is persisted. This selection does not establish historical accuracy or certify every selectable date.

### Unresolved equal-precedence celebrations

When the calendar reports equal-precedence contenders without a ruling, the shared player lets the reader select which celebration's Office to compose.
`celebrationCalendarKey` accepts only a contender's stable key; null keeps the calendar's provisional choice, and a key outside an unresolved tie throws an argument exception.
`Office.SelectedCelebrationCalendarKey` reports an explicit choice. The selected Office does not change the calendar's unresolved occurrence or claim which celebration is observed.
The selection is not persisted and resets when the date or Hour changes.

### Restricted memorial commemoration

GILH 238-239 permits limited commemorations on Lenten weekdays, December 17-24, and days in the Christmas octave.
For Lauds or Vespers, pass the selected `LiturgicalDay.MemorialCommemorations` item's `CalendarKey` as `memorialCommemorationCalendarKey`.
The Hours engine retains the temporal Office and inserts only the memorial antiphon and prayer after its collect; no full memorial Office is substituted.
`Office.SelectedMemorialCommemorationCalendarKey` reports the selected stable key. The choice is not available at Compline, and an unavailable key or unsupported Hour throws an argument exception.
The shared player offers this choice when commemorations are available and resets it when the date, Hour, or calendar changes; the choice is not persisted.
Text providers may supply `proper/{content-id}/commemoration/antiphon` and `proper/{content-id}/commemoration/prayer`; missing text remains directed to official sources.
The Office of Readings' hagiographical reading and responsory are not implemented because that Hour is outside this library's scope. No protected bodies were added.

### Easter-octave Compline form

During Calendar's civil Easter-octave days 1-8, Night Prayer offers either complete Sunday schema, as recorded in `OBS-COMPLINE-01`'s secondary rubric.
Pass `complineForm: ComplineSundayForm.SundayI` or `SundayII` to `Compose`; `Automatic` preserves Sunday I after First Vespers and Sunday II otherwise.
`Office.SelectedComplineForm` exposes the resolved choice only where selectable; it is null for other Hours/dates.
Explicit choices outside that scope and unknown enum values throw argument exceptions rather than silently changing another Office.
The psalms, short reading, concluding prayer, and citation-bound psalmody text keys change together. Calendar's day, title, week, and First Vespers ownership do not change.
The shared player's selector resets prayer progress; date, Hour, and calendar changes restore the date's default form. The choice is not persisted.
This complete-schema choice is distinct from the weekday psalm substitution below; it does not change the octave responsory boundary or certify the complete proper.
The rubric's approved-book check and returned-Office identity review (`AUTH-08`) remain pending; no protected texts were imported.

### Weekday Compline psalms

GILH 88 permits Sunday psalms in place of weekday Compline psalms. GILH 89-90 do not extend that substitution to the reading or concluding prayer.
For Night Prayer using a weekday cycle, pass `complinePsalmody: ComplinePsalmodyChoice.SundayI` (Psalms 4 and 134) or `SundayII` (Psalm 91).
The default, `Weekday`, keeps the existing psalms. `Office.SelectedComplinePsalmody` exposes the current choice where available; otherwise it is null.
Only the psalms and their citation-bound bodies/antiphons use the selected Sunday keys; all other steps retain the weekday cycle and seasonal texts.
The selector is not offered for complete Sunday forms, solemnities, First Vespers, the Triduum, the Easter octave, other Hours, or omitted Offices.
Explicit substitutions outside that scope, conflicting complete-form choices, and unknown enum values fail with argument exceptions.
The shared player resets progress when the choice changes and restores weekday psalms on date, Hour, or calendar changes. No choice is persisted.
This implements the normative option, not certification of every Office's proper texts. No protected bodies or Latin were imported.

### Invitatory before Morning Prayer

GILH 34-35 (PDF p. 14) distinguishes the opening Invitatory verse from its psalm and antiphon; the latter may be omitted before Lauds.
In Hour settings, `InvitatoryMode.OmitPsalm` keeps the existing opening verse, hides the psalm and its antiphon, and leaves the rest of Morning Prayer unchanged.
This is not `None`, which selects the ordinary introduction when the Invitatory was already prayed at an earlier Hour.
The labels explain the condition rather than claiming the app observed an earlier prayer. No prayer history is inferred or tracked.
The new choice is saved as a display/prayer preference through the existing settings service; previous numeric mode values and the default are unchanged.
Switching back to a psalm-selection mode restores its options. OmitPsalm does not advance the daily cycle or alter Evening or Night Prayer.
This implements the Lauds option only, not a separate Office of Readings or a review of every proper Invitatory antiphon. No protected texts or Latin were imported.

### Marian ending at Night Prayer

GILH 92 (PDF p. 20) permits a Marian antiphon at the end of Compline and requires Regina Caeli throughout Easter Time.
Outside Easter, the final step exposes `OfficeStep.Options`: the existing seasonal suggestion first, followed by the other distinct antiphons already in the embedded inventory.
The shared player offers all four regardless of Invitatory preferences; choosing one changes only that ending, not Calendar ownership or any other prayer.
Changing a completed ending makes that step unprayed again. Date, Hour, calendar, and Office-form changes restore the seasonal suggestion; no choice is persisted.
In Easter, the fixed Regina Caeli step remains unchanged and offers no alternatives. An omitted Office has no ending or choice.
This adds no texts and does not certify the existing Latin/unofficial translations or expand the inventory to conference-approved alternatives.

### Partial-verse readings

The existing Hebrews 13:7-9a assignment uses an explicit `OfficeStep.Passage`: verses 7-8 plus the first sentence of verse 9 from the bundled Douay-Rheims text.
It no longer renders all of verse 9. The player labels the excerpt and redirects to an official source if a required part is unavailable.
Inline licensed bodies take precedence unchanged; no reading receives an added Glory Be. This fixes rendering, not the still-unverified assignment or all other partial verses.
Tuesday Compline's 1 Peter 5:8-9a and Friday's Jeremiah 14:9a also use explicit public-domain excerpts; their existing citations remain unchanged.
The latter uses the closing plea in the Douay-Rheims verse, not its opening question. These boundaries are secondary-source-backed, pending approved-book verification.
Sunday-psalm substitution does not change either reading. Complete Sunday schemas retain their own readings without these weekday arrangements.
Week III Wednesday Lauds' Job 1:21; 2:10b likewise excludes the surrounding rebuke and narration from verse 2:10, retaining only its question about good and evil.
Calendar's background Psalter week still selects the reading. The boundary has secondary support; approved-book verification remains pending.
Week IV Saturday's 2 Peter 3:13-15a stops after the opening clause of 15, omitting the continuation about Paul's letters; the same source/verification limits apply.
Week IV Sunday's 2 Timothy 2:8a, 11-13 remains unarranged because the reviewed secondary source's citation and body disagree about verse 8.
Week III Sunday's Ezekiel 37:12b-14 begins at the divine proclamation, excluding the opening instruction to prophesy; the same secondary/approved-book limits apply.
Week II Wednesday Vespers' 1 Peter 5:5b-7 begins at the instruction to mutual humility, excluding the preceding instruction to young men.
The reviewed secondary page gives a longer ending (through verse 11); that assignment discrepancy remains open and the existing citation is unchanged.
Week II Friday Vespers' 1 Corinthians 2:7-10a ends after verse 10's first sentence, omitting the continuation about the Spirit searching all things.
The secondary page corroborates that ending but starts at verse 6; the retained verse 7 start still requires approved-book reconciliation.
Week I Monday Vespers' Colossians 1:9b-11 starts at the petition for knowledge of God's will, omitting the opening report; its ending discrepancy remains open.

The current inventory has 29 distinct suffix-bearing reading citations: 9 retain existing arrangements and 20 resolve through the Bible adapter's whole-verse fallback.
Assigned a/b/c references remain unchanged. For example, `13-15a` may display all of verses 13-15; exact clause extraction is not required for DRB fallback.
`PsalmodyReference.HasPartialVerses` is inventory metadata only, not a rendering gate. Citation-bearing arrangement parts may also use whole boundary verses.
Inline bodies retain precedence and readings receive no added Glory Be. Missing Bible text still produces an official-source notice, regardless of suffixes.
See the data README for the inventory and unresolved assignment-source discrepancies; this policy does not certify assignments or approved liturgical wording.

### Partial-verse psalmody

The same whole-verse policy applies to psalmody, options, and stanzas, including the five suffix-bearing canticle citations listed in the data README.
References, antiphons, and normal doxology behavior are preserved. Existing explicit arrangements, including Daniel's special assembly/ending, retain precedence.
A stanza loads its cited verses, not its full parent psalm. Required option stanzas remain all-or-nothing: a missing, empty, or whitespace-only stanza produces a source notice.
No incomplete stanza set, translation label, or standalone Glory Be is displayed. Independent options and complete supplied bodies remain available.
This streamlines DRB fallback; it does not claim that public-domain wording is an approved Hours translation or that all assignments/endings have been reviewed.

## Feature flag

The whole feature is controlled by `FeatureFlags:LiturgyOfTheHoursFeature` in `src/Respondeo.Wasm/wwwroot/appsettings.json` (default `true`).

When it is `false`:

- the Hours link is removed from the Discover dropdown menu (`DiscoverNavigation`);
- the Hours card is hidden on the Discover overview (`Discover.razor`);
- it no longer counts toward showing the Discover pillar (`PillarNav.razor`).

The `discover/hours` routes stay reachable by direct URL, which matches how the other Discover sub-areas behave.

## Content strategy: the hybrid approach

The official English texts of the Liturgy of the Hours are under copyright.
Respondeo therefore uses a hybrid approach:

1. **Ship public-domain texts now.** Anything in the public domain is included in full.
2. **Link out for copyrighted texts.** When a step's text is copyrighted, `OfficeStep.Text` is left `null`.
   The UI then tells the reader to pray that text from an official source and links to one.
3. **Add licensed texts later, only with permission.** If a rights holder grants written permission (or a fee waiver),
   the text can be added and credited in `THIRD-PARTY-NOTICES.md`.

### Public-domain sources we may use

- Ancient Latin texts: Office hymns, canticles (Benedictus, Magnificat, Nunc Dimittis), the Te Deum, the Marian antiphons
  (Salve Regina, Alma Redemptoris Mater, Ave Regina Caelorum, Regina Caeli), traditional collects, and the fixed versicles
  (for example *Deus, in adiutorium meum intende*).
- Breviary texts published before 1929 (for example the *Breviarium Romanum* and its older English translations).
- The Douay-Rheims Bible (Challoner revision), already shipped via `Respondeo.Bible` and dedicated to the public domain (CC0) in its digital form.
  Its Psalm and chapter numbering follows the Vulgate; check numbering (for example Daniel 3) when citing.
- English translations written by Respondeo contributors.

### Copyrighted sources we must not reproduce without permission

| Text | Rights holder |
| --- | --- |
| *Liturgy of the Hours* English translation (antiphons, responsories, intercessions, prayers, rubrics) | ICEL (International Commission on English in the Liturgy) |
| Revised Grail Psalms / The Grail Psalms | GIA Publications (for the Conception Abbey / The Grail) |
| New American Bible, Revised Edition (NABRE) readings | USCCB / Confraternity of Christian Doctrine |
| Modern compositions: intercessions, newer hymns, new collects | Their respective authors or publishers |

### Translation labeling policy

- Show the Latin original where one exists, followed by the English.
- Any English that Respondeo contributors translated must be clearly labeled as an **unofficial translation**,
  so readers never mistake it for the approved liturgical text.
- Never paraphrase a copyrighted translation closely enough to be a derivative of it.

### Official-text preference

`ILiturgyOfTheHours.Compose` prefers matching provider content by default. Local builds use the existing invented sample provider to exercise the same mechanism.
A caller can disable that content for one composition without changing shared state or subsequent calls:

```csharp
var office = hours.Compose(date, Hour.MorningPrayer);
var fallback = hours.Compose(date, Hour.MorningPrayer, preferOfficialText: false);
```

The override bypasses provider bodies, antiphons, alternative openings, and options. Calendar decisions, references, passage arrangements, endings, and lawful choices do not change.
Existing public-domain bodies remain; Scripture without an inline body resolves through the Bible loader. Unavailable protected elements retain source notices.
This is a caller-level override, not a new persisted player setting. Both hosts use the same default-on engine API.

`OfficeStep.UseOfficialText` remains a missing-content notice flag, not a preference. `IsLicensedText` identifies non-sample provider content;
`IsSampleText` identifies invented provider content. The player labels sample contributions individually, separately from any DRB fallback body.
`HasProviderText` includes nested options so acknowledgments are not lost when only an option uses provider content.
An Office receives sample status and an acknowledgment only when provider content is actually used. Disabling it never suppresses needed missing-content notices.

### Licensed-content pipeline (prepared for an ICEL license)

The engine resolves provider content after assigning the Office, including propers, Commons, fixed elements, Compline, and options.
Licensed bodies can replace dummy content through the existing embedded JSON pipeline; no licensed bodies are acquired by this implementation.

- **Directory of files.** Every `*.json` file in the content directory (except `*.schema.json`) is embedded and merged in ordinal file-name order,
  so a full liturgical year can be split by season, psalter week, sanctoral month, and commons. A key defined in two files is a build-time error.
  Files declare `schemaVersion` (currently 2) and are described by `hours-content.schema.json`.

- **Never committed.** Licensed texts stay out of this public repository. `.gitignore` excludes `.licensed/` and any `hours-content.json`.
Local and fork builds embed `SampleContent/hours/*.sample.json`: invented placeholder wording,
  flagged `"isSample": true`, which makes the player show a "sample text" notice. It is never the ICEL text or a paraphrase of it.
- **Deployment downloads temporarily disabled.** Both private Hours repository checkouts in `deploy.yml` are explicitly disabled,
  so an expired `HOURS_CONTENT_TOKEN` cannot fail web or APK deployments. Both hosts use the committed sample files; licensed-content validation is skipped.
  To restore downloads, configure a valid token and re-enable both checkout conditions together, preserving the APK's pinned `hours-revision`.
  The retained steps check out `Delubear/Respondeo-HoursContent` into `.licensed/hours/`, validate with `HoursContentFileTests` (`HOURS_CONTENT_DIR`),
  and publish with `-p:HoursLicensedContentDir=.../.licensed/hours`.
- **Embedded, not served as a file.** The content is compiled into the assembly as an embedded resource, so it is not a separately downloadable
  static asset. It still ships inside the public WASM bundle, so it is publicly readable in principle; confirm this is acceptable under the license.
- **Non-psalmody lookup keys**, most specific first: `proper/{celebration}/{hour}/{element}`, `proper/{celebration}/{element}`,
  `common/{common}/{hour}/{element}`, `common/{common}/{element}`, `{season}/w{week}/{weekday}/{hour}/{element}`, `{hour}/{element}`, `{element}`.
  `{celebration}` is a stable Hours content ID. `Data/sanctoral.json` maps authored Calendar keys to established IDs, so existing provider files need no migration.
  For example, Calendar key `epiphany` resolves to `the-epiphany-of-the-lord` even if its display name changes; the saint-profile `Id` is never a content binding.
  An unmapped authored key is itself the content ID; it never falls back to a possibly misleading display name. Only entries without `CalendarKey` use legacy name slugification.
  The same resolver drives short readings, proper psalmody, First Vespers canticles, proper body/antiphon keys, and provider Common metadata.
  The citation dataset's Common assignment is authoritative when present; licensed celebration metadata supplies it only when that assignment is absent.
  Elements: `hymn`, `reading`, `responsory`, `gospel-canticle` (body and/or antiphon), `intercessions`, `collect`; psalmody uses its separate binding rules below.
- **Scoped fixed-text keys.** To avoid a generic text replacing a seasonal or night-cycle variant, existing fixed bodies require
  `{season}/w{week}/{weekday}/{hour}/{element}`. This includes fixed responsories/collects, Night Prayer's Gospel canticle, and
  `opening`, `invitatory-verse`, `examen`, `lords-prayer`, and `conclusion`. Lauds' alternative `IntroText` uses `opening`.
  First Vespers fixed keys end in `evening-i`, not `evening`; ordinary Psalter keys retain the existing evening-slot convention.
  Night keys use the selected complete night form's weekday, not the Sunday-psalm substitution weekday. Season/week follow Calendar's existing context.
- **Option keys.** Scoped keys also resolve `invitatory/psalm-95` (or `100`, `67`, `24`) and `marian-antiphon/{text-key}` (for example `salve-regina`).
  Marian keys use the existing stable text identifiers, not display titles. Easter's fixed Regina Caeli follows the same mechanism without adding choices.
  Only sample providers may use unscoped option keys. The dummy Invitatory antiphon now lives in the sample provider, not the ordinary template.
  A supplied full Invitatory body requires its exact canonical citation and includes its ending; antiphon-only content retains the canonical stanza set.
- **Psalmody source binding.** Prescribed proper sets use only hour-specific proper keys; they never fall through to Common or weekday antiphons.
  Feasts using a Common may use its hour-specific keys. Ordinary memorials use weekday psalms with weekday antiphons (GILH 235a).
  Generic psalm-antiphon fallback is reserved for clearly marked sample content, not production.
  Any supplied `citation` must match the selected reference exactly. A nonempty psalmody `text` requires `citation` and must include its entire ending.
  Unbound bodies fail content validation; mismatched bodies or antiphons fail composition rather than appear under the wrong reference.
- **Seasonal opening.** The ordinary introductory verse includes Alleluia outside Lent, but omits it through the pre-Easter Triduum (GILH 41, 85; Universal Norms 28).
  Lauds' alternative opening follows the same rule; its Invitatory verse/options remain unchanged. Easter Sunday's openings include Alleluia through Calendar's season facts.
  This is template selection, not word removal from licensed proper texts. Participation-dependent omissions are handled separately as described above.
- **Holy Week Morning Prayer.** Templates and seasonal text keys use Calendar's `MorningPrayerSeason`, not the aggregate day label.
  Holy Thursday Lauds remains Lenten and uses Thursday Week II psalms (Psalm 80, Isaiah 12:1-6, Psalm 81), not Sunday Week I from its legacy solemnity label.
  Its proper antiphons remain proper: sharing weekday psalms does not authorize weekday antiphons. Missing proper texts remain linked placeholders.
  These references are corroborated by date-selected iBreviary observations (`OBS-LAUDS-01`); approved-book reconciliation remains pending.
  Easter Lauds uses Easter Time text keys while retaining its existing proper psalms and antiphon bindings. Other Triduum morning propers are unchanged.
- **Lenten Sunday Vespers.** Second Vespers uses 1 Peter 2:21-24 instead of Revelation's Alleluia canticle (GILH 137); the two psalms retain Calendar's current week.
  First Vespers keeps Saturday's set and Philippians 2:6-11. Palm Sunday's legacy solemnity rank does not reset either set to Week I: both use Week II.
  Seasonal antiphon keys remain seasonal; Palm Sunday's keys remain proper, with `evening-i` for First Vespers. Missing texts stay placeholders.
  Exact references are corroborated by secondary observations (`OBS-LENT-01`); approved-book reconciliation and other seasonal exceptions remain pending.
- **Epiphany and Transfiguration canticles.** GILH 137 prescribes Timothy at Vespers; both phases use the existing Transfiguration reference, 1 Timothy 3:16.
  Epiphany follows Calendar's selected date, including its US Sunday transfer. The Transfiguration has First Vespers when it falls on Sunday, not on ordinary weekdays.
  First Vespers uses the explicit `firstEveningCanticle` field and its `evening-i` antiphon key; it does not borrow the Second Vespers psalm set or antiphons.
  Exact verse limits and full First Vespers psalm sets remain pending approved-proper review; the existing provisional First Vespers psalms are unchanged.
  Conflicting date-selected digital responses were rejected as new fixtures (`AUTH-08`); settings-date confirmation alone does not establish the returned Office's identity.
- **Easter octave psalmody.** Calendar's selected Office day identifies all eight days; their Lauds uses Psalm 63:2-9, Daniel 3:57-88, 56, and Psalm 149 (GILH 134).
  Vespers uses Psalm 110:1-5, 7, Psalm 114, and Revelation 19:1-7, including First Vespers of the Second Sunday; its background week remains II.
  Exact references are corroborated by secondary date-selected observations (`OBS-OCTAVE-01`); full approved-book/national-proper verification remains pending.
  Easter Sunday's existing psalm-antiphon keys remain unchanged. Days 2-8 use `proper/easter-octave-day-N/{hour}/psalm-antiphon-N`, with `evening-i` for First Vespers.
  These psalmody keys do not authorize weekday or generic production antiphons. Missing proper bodies/antiphons remain explicit placeholders; other elements are not certified.
  Compline retains its separate civil-date response boundary and night cycle; this change does not certify its full octave arrangement.
- **First Vespers and Night Prayer.** Both consume `ILiturgicalCalendar.ForEvening`; Hours no longer decides the evening winner independently.
  Calendar compares Sunday/solemnity precedence and retains today on equality; proper-book exceptions remain under review.
  First Vespers uses `evening-i` for celebration/Common keys, without ambiguous hourless fallbacks.
  Calendar's `ForWeek` supplies the background cycle for the selected Office day: civil day for Morning Prayer, evening owner for Vespers and Compline.
  Metadata, text keys, psalmody, and short readings share that week; Hours no longer calculates or increments weeks independently.
  Sunday psalter keys use the incoming week and Saturday's Evening Prayer slot, not Sunday's Evening Prayer II slot.
  Easter Sunday resets the background cycle to I (GILH 133); proper psalms retain precedence, and Christmas's background lookup remains provisional.
  Compline uses the Sunday-after-I or Sunday-after-II cycle following that decision (GILH 230); ordinary weekday cycles remain unchanged.
  Night Prayer never borrows a saint's proper or Common (GILH 236); its psalter weekday follows the selected night cycle.
  Templates, seasonal text keys, Marian selection, and the Ordinary Time reading gate use Calendar's evening `Season`, not the civil day's daytime season.
  First Advent Vespers cannot borrow an Ordinary Time reading; missing seasonal citations remain explicit proper-content placeholders with official-source links.
  Easter Sunday is also Easter Time for evening texts, despite the preserved day-level Triduum label. Regina Caeli is required in Easter Time (GILH 92).
  Outside Easter Time the existing seasonal Marian choices are suggestions, not mandates; their date-based suggestions retain the civil date.
  Throughout Calendar's eight-day Easter octave, Compline uses a complete Sunday schema rather than weekday psalms, reading, and prayer.
  Sunday II is the default; after First Vespers the existing Sunday I selection remains. The secondary source permits either schema during the octave.
  GILH 88/230 and GNLY 24 support the Sunday-form routing; the exact octave proper still requires approved-book verification (`OBS-COMPLINE-01`, HOUR-05).
  The existing special Compline response is limited to Calendar's civil Easter-octave days 1-7, including Saturday after First Vespers.
  From the Second Sunday through Pentecost, the ordinary Easter responsory requires alleluias; missing licensed text stays an explicit linked placeholder.
  This response boundary follows date-selected iBreviary observations (`OBS-COMPLINE-01`), not completed approved-book verification; HOUR-05 remains pending.
  Correct evening ownership/season does not certify the still-incomplete First Vespers proper psalmody, Triduum responses, or other Office content.
- **Content migration.** Existing hour-specific antiphon-only entries need no `citation` change.
  Add the exact canonical `citation` to any full psalmody body, and split First Vespers texts into `evening-i` keys.
  Move production generic or hourless psalm-antiphon entries into their verified proper/Common/weekday slots.
  Missing entries now produce an official-source notice rather than an unrelated antiphon.
- **Acknowledgment.** The file's `acknowledgment` string is shown directly under the hour's title and again at the end of the page whenever
  licensed text is displayed. ICEL's "Use of ICEL Materials on Global Computer Networks" policy requires it on the first and last
  pages/frames displaying the text. It must name the text, carry ©, the year, ICEL's corporate title, and "All rights reserved", e.g.
  "Excerpts from the English translation of *The Liturgy of the Hours* © 1974, International Commission on English in the Liturgy Corporation. All rights reserved."
- **Fail closed.** If a non-sample file's acknowledgment lacks ©, ICEL, or "All rights reserved" (`HoursAcknowledgment.IsValid`),
  no licensed text is shown and the player falls back to official-source links.
- **Policy conditions (Internet use, no written permission needed).** Only texts approved by the bishops' conference, confirmed by the Holy See,
  and promulgated may be used, and they must be followed exactly. The site must charge no fee to access it or the texts.
  The site must not use the ICEL name in a way that implies affiliation, sponsorship, or endorsement. ICEL may terminate or modify this permission.
  The permission covers the website only, not other forms such as apps or print. Questions go to the ICEL Secretariat: permission@eliturgy.org.
- **Presentation.** Text keeps its authored sense lines; antiphons are marked with a red *Ant.* and repeated after the psalm or canticle;
  psalm headings give both numberings. The psalm text itself stays the public-domain Douay-Rheims until the Grail Psalms are licensed from GIA.

### Official sources we link to

- Universalis: https://universalis.com
- iBreviary: https://www.ibreviary.com
- USCCB Liturgy of the Hours: https://www.usccb.org/prayer-and-worship/liturgy-of-the-hours

### Permission contacts

If we want to add licensed texts later, request written permission (and ask about a fee waiver for a free, non-commercial site) from:

- **ICEL** for the English translation of the Liturgy of the Hours: https://www.icelweb.org
- **GIA Publications** for the Grail Psalms: https://www.giamusic.com
- **USCCB** for the NABRE: https://www.usccb.org/about/permissions

Record any granted permission, its scope, and the required credit line in `THIRD-PARTY-NOTICES.md` before shipping the text.

## Known content gaps

These steps currently render as "pray from an official source" or are not yet modeled:

- Office hymns (public-domain Latin hymns can be added).
- Invitatory antiphons and source-checked assignment; Morning Prayer already offers four psalm choices and displays their selected Scripture text.
- Psalm and canticle antiphons for the four-week psalter and the proper seasons.
- Short readings and short responsories.
- Intercessions (modern composition; copyrighted).
- Concluding prayers for Lauds and Vespers (traditional collects can be added where they exist).
- Compline concluding prayers: six of the seven are now supplied in Latin with unofficial English translations; Thursday still falls back to the official source.
  The Latin of these prayers, the *Salva nos* antiphon, and the Easter *Haec dies* responsory has not yet been checked against a published *Liturgia Horarum* and must be verified before release.

## Disclaimer

This document summarizes our understanding of the licensing landscape to guide development. It is not legal advice.
When in doubt, leave the text out and link to an official source.
