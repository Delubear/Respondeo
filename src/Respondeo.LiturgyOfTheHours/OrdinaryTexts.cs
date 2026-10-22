namespace Respondeo.LiturgyOfTheHours;

/// <summary>
/// Fixed, public-domain texts of the Office: traditional ordinary prayers and Douay-Rheims Scripture.
/// Proper antiphons, hymns, readings, and collects in the modern English edition are copyrighted and are read from official sources instead.
/// </summary>
internal static class OrdinaryTexts
{
    public const string Invitation =
        "V. O God, come to my assistance.\nR. O Lord, make haste to help me.\n\n" +
        "Glory be to the Father, and to the Son, and to the Holy Spirit,\nas it was in the beginning, is now, and ever shall be,\nworld without end. Amen.";

    public const string AlleluiaNote = "Outside Lent, add: Alleluia.";

    public const string GloryBe =
        "Glory be to the Father, and to the Son, and to the Holy Spirit,\nas it was in the beginning, is now, and ever shall be,\nworld without end. Amen.";

    public const string LordsPrayer =
        "Our Father, who art in heaven,\nhallowed be thy name;\nthy kingdom come;\nthy will be done on earth as it is in heaven.\n" +
        "Give us this day our daily bread;\nand forgive us our trespasses,\nas we forgive those who trespass against us;\n" +
        "and lead us not into temptation,\nbut deliver us from evil.\n\nAmen.";

    public const string Benedictus =
        "Blessed be the Lord God of Israel;\nbecause he hath visited and wrought the redemption of his people:\n" +
        "And hath raised up a horn of salvation to us,\nin the house of David his servant:\n" +
        "As he spoke by the mouth of his holy prophets,\nwho are from the beginning:\n" +
        "Salvation from our enemies,\nand from the hand of all that hate us:\n" +
        "To perform mercy to our fathers,\nand to remember his holy testament,\n" +
        "The oath, which he swore to Abraham our father,\nthat he would grant to us,\n" +
        "That being delivered from the hand of our enemies,\nwe may serve him without fear,\n" +
        "In holiness and justice before him,\nall our days.\n" +
        "And thou, child, shalt be called the prophet of the Highest:\nfor thou shalt go before the face of the Lord to prepare his ways:\n" +
        "To give knowledge of salvation to his people,\nunto the remission of their sins:\n" +
        "Through the bowels of the mercy of our God,\nin which the Orient from on high hath visited us:\n" +
        "To enlighten them that sit in darkness, and in the shadow of death:\nto direct our feet into the way of peace.\n\n" + GloryBe;

    public const string Magnificat =
        "My soul doth magnify the Lord.\nAnd my spirit hath rejoiced in God my Saviour.\n" +
        "Because he hath regarded the humility of his handmaid;\nfor behold from henceforth all generations shall call me blessed.\n" +
        "Because he that is mighty hath done great things to me;\nand holy is his name.\n" +
        "And his mercy is from generation unto generations,\nto them that fear him.\n" +
        "He hath shewed might in his arm:\nhe hath scattered the proud in the conceit of their heart.\n" +
        "He hath put down the mighty from their seat,\nand hath exalted the humble.\n" +
        "He hath filled the hungry with good things;\nand the rich he hath sent empty away.\n" +
        "He hath received Israel his servant,\nbeing mindful of his mercy:\n" +
        "As he spoke to our fathers,\nto Abraham and to his seed for ever.\n\n" + GloryBe;

    public const string NuncDimittis =
        "Now thou dost dismiss thy servant, O Lord,\naccording to thy word in peace;\n" +
        "Because my eyes have seen thy salvation,\nwhich thou hast prepared before the face of all peoples:\n" +
        "A light to the revelation of the Gentiles,\nand the glory of thy people Israel.\n\n" + GloryBe;

    public const string NightAntiphon =
        "Salva nos, Domine, vigilantes,\ncustodi nos dormientes;\nut vigilemus cum Christo,\net requiescamus in pace.\n\n" +
        "(Unofficial translation: Save us, O Lord, while we wake, guard us while we sleep;\nthat we may watch with Christ, and rest in peace.)";

    public const string EasterNightResponsory =
        "Haec dies quam fecit Dominus:\nexsultemus et laetemur in ea, alleluia.\n\n" +
        "(Douay-Rheims, Psalm 117:24: This is the day which the Lord hath made: let us be glad and rejoice therein, alleluia.)";

    private const string Unofficial = "\n\n(Unofficial translation: ";

    /// <summary>
    /// Concluding prayers of Night Prayer from the Latin typical edition, keyed by the night of the weekly cycle.
    /// Saturday is the form after Evening Prayer I of Sunday; Sunday is the form after Evening Prayer II.
    /// Nights without a verified Latin text fall back to the official source.
    /// </summary>
    public static readonly IReadOnlyDictionary<DayOfWeek, string> NightConcludingPrayers = new Dictionary<DayOfWeek, string>
    {
        [DayOfWeek.Saturday] =
            "Visita, quaesumus, Domine, habitationem istam,\net omnes insidias inimici ab ea longe repelle;\n" +
            "Angeli tui sancti habitent in ea, qui nos in pace custodiant;\net benedictio tua sit super nos semper.\nPer Christum Dominum nostrum.\n\nAmen." +
            Unofficial + "Visit, we beseech thee, O Lord, this dwelling, and drive far from it all the snares of the enemy;\n" +
            "let thy holy Angels dwell herein to keep us in peace; and may thy blessing be upon us always. Through Christ our Lord. Amen.)",
        [DayOfWeek.Sunday] =
            "Mysterium resurrectionis Domini hodie celebrantes,\nhumiliter te, Domine, deprecamur,\n" +
            "ut in tua pace securi quiescamus,\net in tua laude laeti resurgamus.\nPer Christum Dominum nostrum.\n\nAmen." +
            Unofficial + "Having celebrated today the mystery of the Lord's resurrection, we humbly beseech thee, O Lord,\n" +
            "that we may rest secure in thy peace and rise joyful in thy praise. Through Christ our Lord. Amen.)",
        [DayOfWeek.Monday] =
            "Corporibus nostris, Domine, praebe quietem salubrem,\net seminatam hodie laboris instantia segetem\n" +
            "fac caelesti semine germinare.\nPer Christum Dominum nostrum.\n\nAmen." +
            Unofficial + "Grant our bodies, O Lord, wholesome rest,\nand make the harvest sown today by our labor spring up as heavenly seed. Through Christ our Lord. Amen.)",
        [DayOfWeek.Tuesday] =
            "Illumina, quaesumus, Domine Deus, tenebras nostras,\net totius noctis insidias tu a nobis repelle propitius.\nPer Christum Dominum nostrum.\n\nAmen." +
            Unofficial + "Enlighten our darkness, we beseech thee, O Lord God,\nand in thy mercy drive from us all the snares of the night. Through Christ our Lord. Amen.)",
        [DayOfWeek.Wednesday] =
            "Domine Iesu Christe, qui mitis es et humilis corde,\niugum suave et onus leve sequentibus te proponis:\n" +
            "suscipe, quaesumus, propositiones et opera hodiernae diei,\net requiem nobis concede noctis,\n" +
            "ut in tuo semper servitio promptiores efficiamur.\nQui vivis et regnas in saecula saeculorum.\n\nAmen." +
            Unofficial + "Lord Jesus Christ, meek and humble of heart, who offerest to thy followers a sweet yoke and a light burden:\n" +
            "receive, we beseech thee, the resolutions and works of this day, and grant us rest this night,\n" +
            "that we may be ever more ready in thy service. Who livest and reignest for ever and ever. Amen.)",
        [DayOfWeek.Friday] =
            "Da nobis, omnipotens Deus,\nita Unigenito Filio tuo in morte coniungi,\n" +
            "quatenus per eius sepulcrum ad gloriam resurrectionis valeamus pertingere.\nQui vivit et regnat in saecula saeculorum.\n\nAmen." +
            Unofficial + "Grant us, almighty God, so to be united with thine only-begotten Son in his death,\n" +
            "that through his burial we may come to the glory of the resurrection. Who liveth and reigneth for ever and ever. Amen.)",
    };

    public const string NightResponsory =
        "V. Into thy hands, O Lord, I commend my spirit.\nR. Into thy hands, O Lord, I commend my spirit.\n" +
        "V. Thou hast redeemed us, O Lord, God of truth.\nR. I commend my spirit.\n" +
        "V. Glory be to the Father, and to the Son, and to the Holy Spirit.\nR. Into thy hands, O Lord, I commend my spirit.";

    public const string NightBlessing = "May the all-powerful Lord grant us a restful night and a peaceful death.\n\nAmen.";

    public const string Dismissal =
        "May the Lord bless us, protect us from all evil, and bring us to everlasting life.\n\nAmen.";

    public const string Examen =
        "Pause in silence to review the day: give thanks for its graces, and ask pardon for your sins.\n\n" +
        "I confess to almighty God\nand to you, my brothers and sisters,\nthat I have greatly sinned,\n" +
        "in my thoughts and in my words,\nin what I have done and in what I have failed to do,\n" +
        "through my fault, through my fault,\nthrough my most grievous fault;\n" +
        "therefore I ask blessed Mary ever-Virgin,\nall the Angels and Saints,\nand you, my brothers and sisters,\nto pray for me to the Lord our God.\n\n" +
        "May almighty God have mercy on us,\nforgive us our sins,\nand bring us to everlasting life.\n\nAmen.";

    public const string SalveRegina =
        "Hail, holy Queen, Mother of mercy,\nour life, our sweetness and our hope.\n" +
        "To thee do we cry, poor banished children of Eve;\nto thee do we send up our sighs,\nmourning and weeping in this valley of tears.\n" +
        "Turn then, most gracious advocate,\nthine eyes of mercy toward us;\n" +
        "and after this our exile,\nshow unto us the blessed fruit of thy womb, Jesus.\n" +
        "O clement, O loving, O sweet Virgin Mary.";

    public const string ReginaCaeli =
        "Queen of Heaven, rejoice, alleluia.\nFor he whom thou didst merit to bear, alleluia,\n" +
        "has risen, as he said, alleluia.\nPray for us to God, alleluia.";

    public const string AlmaRedemptoris =
        "Loving Mother of the Redeemer,\ngate of heaven, star of the sea,\nassist thy people who have fallen yet strive to rise again.\n" +
        "To the wonderment of nature thou didst bear thy Creator,\nyet remained a virgin after as before.\n" +
        "Thou who received Gabriel's joyful greeting,\nhave pity on us poor sinners.";

    public const string AveReginaCaelorum =
        "Hail, O Queen of Heaven enthroned.\nHail, by angels mistress owned.\nRoot of Jesse, Gate of morn,\n" +
        "whence the world's true light was born:\nGlorious Virgin, joy to thee,\nloveliest whom in heaven they see:\n" +
        "Fairest thou where all are fair,\nplead with Christ our sins to spare.";
}
