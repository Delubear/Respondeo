# Respondeo.LiturgicalCalendar

A small, self-contained engine that answers a single question: **what does the Church keep on a
given day?** It computes the Ordinary Form (modern Roman general calendar) season, liturgical color,
and principal celebration for a supplied date, behind the public
[`ILiturgicalCalendar`](LiturgicalContracts.cs) interface.

The supported intent is current rules going forward from November 11, 2026, maintained as new decrees are issued.
Earlier dates are computed with the same current rules, not reconstructed historical calendars; no new runtime date restriction or hidden clock is introduced.

The engine is **pure and deterministic**: the caller supplies the date, so there is no hidden clock.
That keeps every consumer testable (the app injects `Func<DateOnly>` for "today") and lets the engine
run as a single shared singleton.

## What it computes

`ILiturgicalCalendar.ForDate(DateOnly)` returns a [`LiturgicalDay`](LiturgicalContracts.cs) with:

| Field | Meaning |
|---|---|
| `Season` | The [`LiturgicalSeason`](LiturgicalContracts.cs): Advent, Christmas, Lent, Triduum, Easter, or Ordinary Time. |
| `Color` | The [`LiturgicalColor`](LiturgicalContracts.cs) of the day (the celebration's color when present, else the season's). |
| `Celebration` | The principal named [`LiturgicalCelebration`](LiturgicalContracts.cs) (name, rank, color, optional saint-profile `Id`/`Summary`), or `null` on a ferial day. |
| `FerialName` | The ferial description when there is no principal celebration, e.g. "Monday of the Third Week of Lent". |
| `OptionalMemorials` | Memorials available as full Office choices; Sundays, solemnities, feasts, and privileged weekdays do not expose them here. |
| `MemorialCommemorations` | Memorials that may be commemorated in the limited form permitted on privileged weekdays (GILH 238-239). |
| `Occurrence` | The decisive typed occurrence rule, stable `CAL-02/...` identifier, and candidates suppressed at that stage; null for manually supplied days without diagnostics. |
| `RankAdjustments` | Candidate-generation rank changes with the original celebration, old/new ranks, and stable rule ID. |
| `Precedence` | The resolved observance's named category from the Universal Norms 59 table, combining temporal facts and the principal celebration. |
| `Obligation` | Typed Mass-obligation status and rule identifier for the selected calendar profile; it is independent of celebration rank. |
| `MorningPrayerSeason` | Lauds' season: Holy Thursday remains Lenten; Easter Sunday is Easter Time despite the overlapping Triduum. Other dates retain `Season`. |
| `IsHolyWeekWeekday` | Monday through Thursday of Holy Week, independent of legacy celebration rank (Universal Norms 16a). |
| `EasterOctaveDay` | The date's ordinal from Easter Sunday (1) through the Second Sunday (8), otherwise null (Universal Norms 24). |

The General Roman profile applies the universal list in Canon 1246 §1. The United States profile provisionally applies the national list and Saturday/Monday exceptions;
direct USCCB decree-level verification remains open. Sundays are always identified as obligatory. The Ascension option controls whether Thursday is separately obligatory;
the Sunday-observance profile remains obligatory on Sunday. The Saturday/Monday exception is applied to January 1, August 15, and November 1.
If an obligatory solemnity is transferred, the result is unknown on the dates affected because transfer of the Mass obligation is not established by this model.
These are calendar-profile facts, not local diocesan dispensations or individual advice.

The existing aggregate `Season` still labels Holy Thursday through Easter Sunday as Triduum; use the morning/evening facts for Office composition.
`MorningPrayerSeason` follows Universal Norms 19, 22, 28. It does not change celebration identity, day color, or participation-dependent Office omissions.
`LiturgicalEvening.EasterOctaveDay` delegates to `Today`, preserving the civil date; use `OfficeDay.EasterOctaveDay` for the selected Vespers proper.

`ILiturgicalCalendar.ForWeek(DateOnly)` returns [`LiturgicalWeek`](LiturgicalWeek.cs): the background `TemporalWeek` and four-week `PsalterWeek`.
Calendar's Ordinary Time names and Hours share one calculation across both spans, including the regional Baptism boundary and the resumption after Pentecost.
Advent, Lent, and Easter follow GILH 133; Easter Sunday resets to I despite its preserved day-level Triduum label.
`TemporalWeek` is null for Christmas, the Triduum, and the days after Ash Wednesday. Christmas's existing background lookup convention remains provisional.
`ForWeek(DateOnly.MinValue)` throws because its preceding Christmas is outside the representable date range.
These are background facts, not permission to replace proper psalmody or Compline's weekly forms. Approved-book seasonal/proper reconciliation remains pending.
Both new interface methods have default implementations so existing calendar implementers remain compatible.

`ILiturgicalCalendar.ForEvening(DateOnly)` returns a [`LiturgicalEvening`](LiturgicalEvening.cs): the civil `Today`, selected `OfficeDay`, `Phase`, and evening `Season`.
The phase distinguishes ordinary Evening Prayer, First Vespers, Second Vespers, and the Triduum's proper Office.
The evening season follows the selected Office, not an automatic tomorrow rollover: Advent/Christmas begin at First Vespers, but Lent starts on Ash Wednesday.
Easter Sunday uses Easter Time for evening texts while retaining its Triduum-proper phase and existing day-level season (Universal Norms 19, 22).
Pentecost retains Easter Time through Compline; Ordinary Time resumes on Monday. These are temporal-text facts, not permission to omit a proper Office.
`EasterOctaveDay` identifies the civil day's ordinal within the eight-day octave (1-8 or null; Universal Norms 24).
It does not advance with First Vespers: Easter Saturday remains day 7 even when the evening Office belongs to the Second Sunday, day 8.
Hours applies element-specific rules to this fact; the calendar does not imply that every octave element continues through Sunday Compline.
Both Vespers and Compline consume this decision; Morning Prayer remains on the civil day. Existing calendar implementations inherit a default resolver.
`ForEvening(DateOnly.MaxValue)` throws because evening concurrence requires the following civil date.
For supported Sunday/solemnity concurrence, higher precedence wins and equality keeps today's Vespers (Universal Norms 59, 61).
Privileged Sundays and Christmas/Epiphany/Ascension/Pentecost are distinguished from ordinary solemnities; the region controls transferred Epiphany and Ascension.
This bounded comparison is not the complete occurrence table or a local-proper model. Triduum participation-dependent omissions are not yet modeled.
All Souls, special propers, complete First Vespers content, and approved-book verification of the full Holy Week Office still require review.

Moveable feast dates are computed from Easter by explicit domain rules in `RomanCalendar`; their names, ranks,
colors, stable keys, and precedence are authored in [`ComputedCelebrations.txt`](ComputedCelebrations.txt).
The validated catalog loader is [`ComputedCelebrationData`](ComputedCelebrationData.cs). Fixed General Roman
Calendar days remain authored in [`GeneralRomanCalendar.txt`](GeneralRomanCalendar.txt) and loaded by
[`GeneralRomanCalendarData`](GeneralRomanCalendarData.cs). This keeps calendar facts declarative without
moving date arithmetic or collision policy into an untyped rules language.

[`CalendarProfile`](CalendarProfile.cs) resolves public options into catalog selection, Epiphany placement, Ascension and Corpus Christi offsets,
Thanksgiving inclusion, prayer-day transfer policy, and obligation-policy selection. Calendar dates, week numbering, and obligation resolution share that policy.
The local patron option remains an explicit input to the transfer resolver; profiles do not establish diocesan membership or historical law.

[`CalendarCatalog`](CalendarCatalog.cs) validates the selected profile once when a calendar is constructed, before any date is queried.
It checks required computed identities, fixed identities and nominal dates used by transfers, and cross-catalog identity conflicts.
Epiphany's fixed and Sunday-placement definitions must agree on name, rank, color, and precedence. Regional overrides remain intentional profile facts.
Both catalog loaders publish frozen dictionaries; fixed-date celebration lists are read-only snapshots.
Authored `LiturgicalCelebration` records describe definitions, not mutable daily state. Rank adjustments create copies and retain the original in diagnostics;
transfers attach nominal/observed dates to `LiturgicalDay.Transfers` without changing the catalog definition. These boundaries preserve existing public contracts.

`RomanCalendar.ForDate` visibly orders temporal-season calculation, candidate generation/transfers, pure occurrence selection, and color/ferial presentation.
[`LiturgicalOccurrenceResolver`](LiturgicalOccurrence.cs) owns the extracted selection rules; it does not read catalogs, transfer celebrations, or depend on Hours/content.
Selection compares named [`LiturgicalPrecedenceClass`](LiturgicalPrecedence.cs) categories against the temporal day; enum numbers are the normative table's row numbers, not invented priorities.
Explicit categories distinguish feasts of the Lord, Ash Wednesday, the Triduum, principal temporal solemnities, and All Souls despite their legacy rank labels.
The blanket moveable-celebration Lord flag and fixed Sunday-replacement date whitelist are removed. A relocated or renamed celebration retains its authored category.
`Occurrence` reports the decisive branch and candidates excluded by selection. All Souls has its own `CAL-02/all-souls` diagnostic, not a Lord-feast diagnostic.
It does not report candidates excluded earlier or nominal/transfer dates; `RankAdjustments` separately records candidate-generation rank changes.
Evening concurrence shares these categories, but only eligible Sundays/solemnities compete; a Saturday feast has no Second Vespers to displace Sunday's First Vespers.
The documented Mother of the Church precedence is explicit. Other equal winning-class ties are marked `CAL-02/unresolved-equal-precedence`.
The first candidate in an unresolved tie is provisional, not an asserted liturgical winner. `ContendingCelebrations` lists every tied candidate separately from `SuppressedCelebrations`,
so unresolved contenders are not misreported as excluded. Scoped tie rules remain pending.

`LiturgicalCelebration.CalendarKey` is an authored identity separate from display `Name` and saint-profile `Id`; `Precedence` overrides rank-based general categories.
Keys/categories cover computed celebrations, all 222 fixed General entries, and all 22 celebrations in the bundled US proper.
The Immaculate Heart collision rule uses its stable key instead of a display-name substring. Its rank reduction preserves the key, clears the obligatory category,
and reports each affected celebration's original and adjusted ranks through `LiturgicalDay.RankAdjustments`.
Transfers identify the nominal catalog entry by key and reuse its entire regional record, preserving summaries, profile links, and precedence instead of reconstructing a display name.
Only a solemnity with an applicable transfer rule is removed from its nominal date's candidate list.
Transfer targets are resolved by the pure `LiturgicalTransferResolver`. Corpus Christi, the Nativity of Saint John the Baptist, and the Sacred Heart are General Calendar solemnities
in the same precedence-table class. When Corpus Christi coincides with John on June 24, the engine leaves both as unresolved contenders rather than inventing a transfer;
the US Sunday-observance cases follow the same policy. John the Baptist and the Sacred Heart have a separate pair-specific collision rule, with an explicit local-patron setting
for the reversed direction. When impeded by the Sacred Heart on June 29, Saints Peter and Paul move to the next day whose temporal precedence and named celebrations do not fall
in classes 1-8. Memorials do not block the transfer; the reported 1973 example lands on June 30. These are pair-specific rules, not generalized movable-solemnity transfers.
`LiturgicalDay.Transfers` exposes the typed rule and nominal/observed dates on both dates, with stable `CAL-03/...` identifiers. Day equality compares memorial and diagnostic sequences structurally.
For the Peter and Paul/Sacred Heart search, `SkippedDateRange` reports any contiguous unavailable dates before the observed date; it is null when the first target is available.
The reported 1973 Sacred Heart collision is covered: Saints Peter and Paul move to the next available day (June 30 in that example), with recurring collisions checked across the Gregorian cycle.
The 2035 Annunciation transfer target is tested against Saint Francis of Paola's memorial. A 400-year scan (2000-2399) found no transferred solemnity
that failed to become principal on its observed date. The scan found four General Thursday Corpus Christi/John the Baptist ties and ten US Sunday ties in each Ascension profile.
All remain unresolved because the precedence table does not break their equal-class tie. Corpus Christi fell on June 3 in 2021, not June 24. Other occupied destinations remain open.
The US prayer day's Sunday-to-Monday move now uses its key rather than a name prefix; it retains its Violet color, legacy Memorial rank, and explicit ProperMemorial category.
US obligatory memorials use ProperMemorial; Guadalupe uses ProperFeast. Optional memorials retain table row 12.
Four relocated and three rank-overridden General celebrations share their identity with the US entry; removals still match date/name.
All General fixed entries have explicit keys/categories; the bundled US inventory is complete, not a complete diocesan proper.
Hours preserves existing content IDs for all 254 migrated identities: 30 compatibility aliases and 224 direct content IDs, independent of display names.
Unmapped authored keys are content IDs; only entries without a key retain legacy name-derived Hours lookup. Calendar does not depend on Hours' binding catalog.
Transfer collisions, restricted commemorations, and complete local-proper certification remain open.
`LiturgicalOccurrenceTests` and `LiturgicalPrecedenceTests` cover branches, table boundaries, renaming/relocation, All Souls, and Saturday-feast evening eligibility.
All three calendar profiles and existing source-backed date fixtures remain in the regression suite.

## Using it

Register the engine from the consuming app's DI container and depend only on the interface; the
implementation stays internal:

```csharp
using Respondeo.LiturgicalCalendar;

builder.Services.AddRespondeoLiturgy();
```

```csharp
public MyService(ILiturgicalCalendar calendar)
{
    var today = calendar.ForDate(DateOnly.FromDateTime(DateTime.Today));
    // today.Season, today.Color, today.Celebration?.Name, ...
}
```

In the main app the calendar is the source of truth for two features:
[`FeastOfTheDay`](../Respondeo.UI/Services/Calendar/FeastOfTheDay.cs) (the "today the Church celebrates…"
saint highlight, linked to `discover/saints/{Id}`) and
[`SeasonalPortrait`](../Respondeo.UI/Services/Calendar/SeasonalPortrait.cs) (the masthead Aquinas portrait that
changes for Christmas Time and Ash Wednesday).

Besides the General Roman Calendar, a USCCB (United States) calendar is available; readers choose it from the calendar dialog.

## Editing the calendar data

To add or correct a fixed celebration, edit [`GeneralRomanCalendar.txt`](GeneralRomanCalendar.txt)
rather than the C# engine. Candidate generation and transfers live in [`RomanCalendar`](RomanCalendar.cs);
occurrence selection lives in [`LiturgicalOccurrenceResolver`](LiturgicalOccurrence.cs).
Cover changes with tests in `Respondeo.UnitTests`, which drive pure stages and `ForDate` with explicit dates so the engine stays deterministic.

The transfer regressions sweep the 2000-2399 Gregorian cycle for each calendar profile.
Destination checks cover all 44 Annunciation transfers and 22 Saint Joseph anticipations that land on fixed memorial dates,
plus all 57 Immaculate Conception transfers that land on Saint Juan Diego's optional memorial.
Privileged-Sunday transfers cover 43 Saint Joseph and 36 Annunciation dates; Annunciation Holy Week/Easter-octave transfers cover all 107 recurring dates.
The John/Sacred Heart pair's two directions cover all 15 collision years.
Saints Peter and Paul move to the next available day in all 10 recurring Sacred Heart collisions; June 30 is the destination in the 1973 fixture.
The Immaculate Heart rule covers 80 obligatory-memorial overlaps.
Mother of the Church covers 73 obligatory-memorial ties and 209 General-calendar or 222 US-calendar dates overlapping any fixed memorial.
These are consistency checks, not broader policy or authority evidence.

The embedded loader rejects malformed rows with the resource name and physical line number instead of returning a partial calendar.
Dates must be real `MM-DD` dates (February 29 is allowed); ranks/colors must be defined enum names, not numbers or combined values; celebration names must not be blank.
The universal baseline follows [Canon 1246-1248](https://www.vatican.va/archive/cod-iuris-canonici/eng/documents/cic_lib4-cann1244-1253_en.html).
The [authority register](../../docs/liturgical-authority-baseline.json) records the USCCB annual calendars as partial evidence; they do not fully establish the U.S. holy-day rules here.
The United States result does not determine diocesan dispensations or other local law.
Each catalog permits distinct celebrations on the same date, but not duplicate date/name pairs. Regional operation matching remains exact and case-sensitive during identity migration.
The US proper permits one operation per date/name pair: add, replace, or remove. A removal must match exactly one General entry; conflicting operations are rejected.
Legacy rows have 4-6 fields; extended rows have eight: `MM-DD | Rank | Color | Name | SaintSlug | Summary | CalendarKey | Precedence`.
Keep empty profile/summary fields. Keys must be nonempty lowercase kebab-case; precedence must be a named enum value compatible with the legacy rank.
Duplicate keys within or across merged catalogs are rejected. A same-name regional replacement cannot change an existing stable key.
Same-name replacements inherit the profile, key, and omitted summary; an omitted precedence is inherited only when rank is unchanged.
Catalog rows are indivisible records; their existing one-line summaries are preserved even where an extended machine-data row exceeds the prose line-length limit.
Catalog fingerprints track metadata additions on all 222 General entries and all 22 US celebrations; existing calendar fields, 222 General rows, and 26 US operations are unchanged.
`LiturgicalCalendarDataTests` exercises this production loader with in-memory catalogs; the baseline tests additionally verify every actual embedded entry and its metadata.
`LiturgicalTransferIdentityTests` checks whole-record preservation, nominal-date suppression, single occurrence, and unimpeded dates across all profiles.
`LiturgicalUnitedStatesIdentityTests` checks all 22 US identities/categories, four regional relocations, three rank overrides, and retained metadata.
`LiturgicalFixedFeastIdentityTests` checks all 222 fixed identity/category records, the 35 solemnities/feasts, Sunday precedence, and transferred Hours IDs.
The future transfer fixtures exercise the existing rules, including Joseph's anticipation and June collisions; they are not independent certification of those future calendars.
These checks validate data integrity, not the liturgical authority or completeness of the assignments.
Both fixed and computed loaders share `CalendarDataValidation` for lowercase kebab-case keys, defined enum names, and rank/precedence compatibility.
Each loader retains its resource-specific diagnostics and physical line numbers; fixed legacy rows without authored metadata remain supported.

## Authority baseline and reconciliation

The [implementation plan](../../docs/liturgical-calendar-and-hours-plan.md) separates calendar authority from Hours element selection.
The [evidence register](../../docs/liturgical-authority-baseline.json) records source fingerprints, rule-review status, open issues, and independent 2026-2028 date fixtures.
It is an in-progress baseline, not a claim that the current calendar or Hours are complete. Candidate years 2026-2028 are not certified support boundaries or runtime restrictions.
Review `CAL-06` and issue `AUTH-09` explicitly retain the provisional US obligation policy and pending direct decree review.
Review `HOUR-02` identifies the extracted temporal citation assignments and their pending approved-book check; structural validation does not certify their authority.

An obligatory memorial excludes competing memorial options. For example, Mary, Mother of the Church takes precedence over Saint Boniface on June 5, 2028; Boniface is omitted, not made optional.
This follows the March 24, 2018 CDW notification and the USCCB 2028 calendar, printed p. 7 note 6e.
The different Immaculate Heart collision rule is preserved: when it meets another obligatory memorial, both become optional before the day's principal is selected.
On privileged weekdays, `MemorialCommemorations` lists memorials separately from full Office choices, following GILH 238-239.
Hours can add the selected memorial's antiphon and prayer after the temporal collect at Lauds or Vespers; the Office of Readings commemoration remains unsupported.
`LiturgicalMemorialOccurrenceTests` checks these distinctions in all three profiles and sweeps 2027-2040 under current rules.
The Saturday Marian Office option, complete national/proper category assignments, and special evening rules remain pending.
Six rule-derived evening fixtures separately cover April 4, 2027 and January 1, 2028 in all profiles; these are not transcribed ordines or full-calendar certification.

`LiturgicalAuthorityBaselineTests` checks all 222 fixed rows, the 26 US operations (19 additions, three replacements, four removals), and every embedded month/day lookup in both regions.
These are repository-consistency checks, distinct from the source-backed date fixtures. The remaining calendar dates, Hours assignments, and current jurisdiction policies need independent review.
Catalog fingerprints normalize non-comment rows by trimming fields, joining fields with `|`, and joining rows with LF, UTF-8 encoded without a final newline.
They ignore line endings/comments but include names, ranks, colors, profile links, and summaries. Data changes require deliberate baseline review, never automatic approval by regenerating a hash.

The [amendment register](../../docs/liturgical-calendar-amendments.json) records nine official decrees covering 11 reviewed nominal assignments,
including Teresa and Newman's universal optional memorials, now inherited by the US calendar rather than duplicated in its overlay.
Newman's title and name-derived Hours key are Priest and Doctor; the permitted Pastors default is preserved.
`LiturgicalCalendarAmendmentTests` requires all reviewed nominal assignments to match in both regions, with no known-discrepancy allowances.
It also tests optional rank, Sunday suppression, and Hours lookup continuity. These checks are not certification of the entire calendar or full Office assignments.
The permitted Common alternatives are recorded, not yet fully modeled; full Office propers and the remaining current national provisions need review.
Historical effective-date reconstruction is out of scope and does not block current-rule corrections; future decrees/policies still need versioned updates.

The content scope is exact Psalm/Scripture/canticle references, public-domain Bible excerpts, and labeled placeholders for protected bodies.
Optional Latin must have verified source/edition rights and remain independently filterable/removable; neither Latin nor online availability establishes public-domain status.
