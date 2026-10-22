# Respondeo.LiturgyOfTheHours

Builds the step-by-step order of the Liturgy of the Hours (Ordinary Form) used by the Discover > Hours pages.

## Scope

- **Hours covered:** Morning Prayer (Lauds), Evening Prayer (Vespers), and Night Prayer (Compline).
- **Not covered (yet):** Office of Readings, Daytime Prayer (Terce, Sext, None), and the Invitatory as a separate hour.
- The site is public, free, and ad-free. Being non-commercial does **not** by itself grant permission to reproduce copyrighted liturgical texts.

## Feature flag

The whole feature is controlled by `FeatureFlags:LiturgyOfTheHoursFeature` in `src/Respondeo/wwwroot/appsettings.json` (default `true`).

When it is `false`:

- the Hours tab is hidden from the Discover tab strip (`DiscoverNavigation`);
- the Hours card is hidden on the Discover overview (`Discover.razor`);
- it no longer counts toward showing the Discover pillar (`PillarNav.razor`).

The feature is also hidden whenever the `DiscoverPillar` master switch is off.
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
- Invitatory and Psalm 95 with its antiphons.
- Psalm and canticle antiphons for the four-week psalter and the proper seasons.
- Short readings and short responsories.
- Intercessions (modern composition; copyrighted).
- Concluding prayers for Lauds and Vespers (traditional collects can be added where they exist).
- Compline concluding prayers: six of the seven are now supplied in Latin with unofficial English translations; Thursday still falls back to the official source.
  The Latin of these prayers, the *Salva nos* antiphon, and the Easter *Haec dies* responsory has not yet been checked against a published *Liturgia Horarum* and must be verified before release.

## Disclaimer

This document summarizes our understanding of the licensing landscape to guide development. It is not legal advice.
When in doubt, leave the text out and link to an official source.
