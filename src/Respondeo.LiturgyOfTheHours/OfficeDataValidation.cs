using System.Text.RegularExpressions;

namespace Respondeo.LiturgyOfTheHours;

/// <summary>Checks authored links without requiring optional proper texts or licensed provider content.</summary>
internal static partial class OfficeDataValidation
{
    internal static void Validate(OrdinaryTextsData texts, HoursData hours, SanctoralData sanctoral)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var active = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in texts.Texts.Keys)
        {
            Visit(key);
        }

        foreach (var (key, lines) in texts.NightConcludingPrayers)
        {
            CheckLines(lines, $"{OfficeData.TextsResource}: nightConcludingPrayers.{key}");
        }

        foreach (var rule in texts.MarianAntiphons)
        {
            RequireText(rule.Text, $"{OfficeData.TextsResource}: marianAntiphons.{rule.Title}");
        }

        foreach (var (hour, template) in hours.Hours)
        {
            for (var index = 0; index < template.Steps.Count; index++)
            {
                var step = template.Steps[index];
                var location = $"{OfficeData.HoursResource}: hours.{hour}.steps[{index}]";
                CheckLines(step.Text, location + ".text");
                CheckLines(step.IntroText, location + ".introText");
                CheckLines(step.Antiphon is { } antiphon ? [antiphon] : null, location + ".antiphon");
                foreach (var (season, seasonOverride) in step.Seasons ?? [])
                {
                    CheckLines(seasonOverride.Text, location + $".seasons.{season}.text");
                    CheckLines(seasonOverride.IntroText, location + $".seasons.{season}.introText");
                }
            }
        }

        foreach (var (id, readings) in sanctoral.Commons.Concat(sanctoral.Celebrations))
        {
            if (readings.Common is { } common && !sanctoral.Commons.ContainsKey(common))
            {
                throw Invalid(OfficeData.SanctoralResource, $"'{id}' references unknown Common '{common}'");
            }
        }

        foreach (var (key, id) in sanctoral.CalendarBindings)
        {
            if (!Identity().IsMatch(key) || !Identity().IsMatch(id))
            {
                throw Invalid(OfficeData.SanctoralResource, $"Invalid calendar binding '{key}' -> '{id}'; expected lowercase kebab-case identities");
            }
        }

        void Visit(string key)
        {
            if (visited.Contains(key))
            {
                return;
            }

            RequireText(key, OfficeData.TextsResource);
            if (!active.Add(key))
            {
                throw Invalid(OfficeData.TextsResource, $"Cyclic text reference involving '{key}'");
            }

            CheckLines(texts.Texts[key], $"{OfficeData.TextsResource}: texts.{key}");
            active.Remove(key);
            visited.Add(key);
        }

        void RequireText(string key, string location)
        {
            if (!texts.Texts.ContainsKey(key))
            {
                throw Invalid(location, $"Unknown ordinary text '{key}'");
            }
        }

        void CheckLines(IEnumerable<string>? lines, string location)
        {
            if (lines is null)
            {
                return;
            }

            foreach (var line in lines)
            {
                if (line is null)
                {
                    throw Invalid(location, "Text lines must not be null");
                }

                var expanded = BreviaryText.ExpandTokens(line);
                var remainder = TextReference().Replace(expanded, match =>
                {
                    var key = match.Groups[1].Value;
                    RequireText(key, location);
                    Visit(key);
                    return string.Empty;
                });
                if (remainder.Contains('{') || remainder.Contains('}'))
                {
                    throw Invalid(location, $"Unrecognized text reference or breviary token in '{line}'");
                }
            }
        }
    }

    /// <summary>Integration validation against the union of supported calendar catalogs, not resolved winners on sampled dates.</summary>
    internal static void ValidateCalendarBindings(SanctoralData sanctoral, IEnumerable<string> calendarKeys)
    {
        var known = calendarKeys.ToHashSet(StringComparer.Ordinal);
        foreach (var key in sanctoral.CalendarBindings.Keys)
        {
            if (!known.Contains(key))
            {
                throw Invalid(OfficeData.SanctoralResource, $"Binding references unknown calendar identity '{key}'");
            }
        }
    }

    private static InvalidOperationException Invalid(string source, string message) => new($"Invalid Office data in '{source}': {message}.");

    [GeneratedRegex(@"\{([a-z0-9-]+)\}")]
    private static partial Regex TextReference();

    [GeneratedRegex(@"\A[a-z0-9]+(?:-[a-z0-9]+)*\z")]
    private static partial Regex Identity();
}
