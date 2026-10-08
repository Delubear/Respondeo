# Respondeo.LiturgicalCalendar

A small, self-contained engine that answers a single question: **what does the Church keep on a
given day?** It computes the Ordinary Form (modern Roman general calendar) season, liturgical color,
and principal celebration for any date, behind the public
[`ILiturgicalCalendar`](LiturgicalContracts.cs) interface.

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
| `OptionalMemorials` | Optional memorials available that day (empty on most days; suppressed on Sundays, solemnities, feasts, and privileged weekdays). |

Moveable feasts are anchored on Easter, which is computed with [`Computus`](Computus.cs). The fixed
General Roman Calendar days are authored in [`GeneralRomanCalendar.txt`](GeneralRomanCalendar.txt) and
loaded by [`GeneralRomanCalendarData`](GeneralRomanCalendarData.cs); the ranking and precedence rules
live in the internal [`RomanCalendar`](RomanCalendar.cs) engine.

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
[`FeastOfTheDay`](../Respondeo/Services/FeastOfTheDay.cs) (the "today the Church celebrates…"
saint highlight, linked to `discover/saints/{Id}`) and
[`SeasonalPortrait`](../Respondeo/Services/SeasonalPortrait.cs) (the masthead Aquinas portrait that
changes for Christmas Time and Ash Wednesday).

## Editing the calendar data

To add or correct a fixed celebration, edit [`GeneralRomanCalendar.txt`](GeneralRomanCalendar.txt)
rather than the C# engine. To change how days are ranked or how precedence is resolved, edit
[`RomanCalendar`](RomanCalendar.cs). Cover any change with tests in `Respondeo.UnitTests`, which drive
`ForDate` with injected dates so the engine stays deterministic.
