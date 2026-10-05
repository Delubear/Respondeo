namespace Respondeo.Content.Liturgy;

/// <summary>
/// Computes the date of Easter Sunday, the anchor from which every moveable feast in the Western
/// liturgical year is derived (Ash Wednesday, Pentecost, Corpus Christi, and the rest).
/// </summary>
internal static class Computus
{
    /// <summary>
    /// Returns the date of Easter Sunday in the given <paramref name="year"/> for the Gregorian calendar,
    /// using the Anonymous Gregorian algorithm ("Meeus/Jones/Butcher").
    /// </summary>
    public static DateOnly GregorianEaster(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = ((19 * a) + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + (2 * e) + (2 * i) - h - k) % 7;
        var m = (a + (11 * h) + (22 * l)) / 451;
        var month = (h + l - (7 * m) + 114) / 31;
        var day = ((h + l - (7 * m) + 114) % 31) + 1;

        return new DateOnly(year, month, day);
    }
}
