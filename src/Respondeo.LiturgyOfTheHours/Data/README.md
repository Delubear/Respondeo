# Liturgy of the Hours data

## sanctoral.json

Short-reading citations for Morning and Evening Prayer on celebrations of the saints.
Citations were scraped from [liturgies.net](https://www.liturgies.net/Liturgies/Catholic/loh/commons.htm)
(commons pages and the `/saints/{slug}/` pages that have hour pages).

- `commons`: the readings of each Common.
- `celebrations`: keyed by the celebration id (the slugified name from the calendar data).
  Each entry gives its own `morning`/`evening` citations, a `common`, or both.
  A celebration with no entry, or no citation for an hour, falls back to the official source link.
- `eveningPsalmody`: the psalms and canticle of Evening Prayer on a feast or solemnity, set on a Common or a celebration.
  A celebration's own list wins; otherwise its Common's list is used. It does not apply when the evening is First Vespers
  of a Sunday or solemnity, and festal Morning Prayer always uses the psalmody of Sunday, Week I.

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
   - Saint Martin of Tours: the site page has no Evening Prayer reading.
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
