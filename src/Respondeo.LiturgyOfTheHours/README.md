# Respondeo.LiturgyOfTheHours

Builds the step-by-step order of the Liturgy of the Hours (Ordinary Form) used by the Discover > Hours pages.

## Scope

- **Hours covered:** Morning Prayer (Lauds), Evening Prayer (Vespers), and Night Prayer (Compline).
- **Not covered (yet):** Office of Readings, Daytime Prayer (Terce, Sext, None), and the Invitatory as a separate hour.
- The site is public, free, and ad-free. Being non-commercial does **not** by itself grant permission to reproduce copyrighted liturgical texts.

## Feature flag

The whole feature is controlled by `FeatureFlags:LiturgyOfTheHoursFeature` in `src/Respondeo/wwwroot/appsettings.json` (default `true`).

When it is `false`:

from the Discover dropdown menu (`DiscoverNavigation`)
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

### Licensed-content pipeline (prepared for an ICEL license)

The engine already composes the licensed proper texts (hymns, psalm and Gospel canticle antiphons, readings, responsories,
intercessions, and collects) from a directory of JSON content files, so a license only changes the data, never the code.

- **Directory of files.** Every `*.json` file in the content directory (except `*.schema.json`) is embedded and merged in ordinal file-name order,
  so a full liturgical year can be split by season, psalter week, sanctoral month, and commons. A key defined in two files is a build-time error.
  Files declare `schemaVersion` (currently 2) and are described by `hours-content.schema.json`.

- **Never committed.** Licensed texts stay out of this public repository. `.gitignore` excludes `.licensed/` and any `hours-content.json`.
Local and fork builds embed `SampleContent/hours/*.sample.json`: invented placeholder wording,
  flagged `"isSample": true`, which makes the player show a "sample text" notice. It is never the ICEL text or a paraphrase of it.
- **Injected at deploy.** `deploy.yml` checks out the private repository `Delubear/Respondeo-HoursContent` (secret `HOURS_CONTENT_TOKEN`)
  into `.licensed/hours/`, validates it with `HoursContentFileTests` (`HOURS_CONTENT_DIR`), and publishes with `-p:HoursLicensedContentDir=.../.licensed/hours`.
  Without the secret, the deployment falls back to the sample files.
- **Embedded, not served as a file.** The content is compiled into the assembly as an embedded resource, so it is not a separately downloadable
  static asset. It still ships inside the public WASM bundle, so it is publicly readable in principle; confirm this is acceptable under the license.
- **Lookup keys**, most specific first: `proper/{celebration}/{hour}/{element}`, `proper/{celebration}/{element}`,
  `common/{common}/{hour}/{element}`, `common/{common}/{element}`, `{season}/w{week}/{weekday}/{hour}/{element}`, `{hour}/{element}`, `{element}`.
  `{celebration}` is the celebration name slugified with diacritics removed (e.g. `saint-therese-of-the-child-jesus`);
  the `celebrations` map assigns a celebration its common (e.g. `"our-lady-of-lourdes": { "common": "blessed-virgin-mary" }`).
  Elements: `hymn`, `psalm-antiphon-1..3`, `reading`, `responsory`, `gospel-canticle` (antiphon), `intercessions`, `collect`.
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
