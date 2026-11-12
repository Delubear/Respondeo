using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class OpeningAlleluiaTests
{
    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void Openings_follow_the_selected_Office_season_without_a_conditional_rubric(string profile)
    {
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)), new NoTextProvider());
        var cases = new (DateOnly Date, bool Alleluia)[]
        {
            (new(2026, 11, 28), true),
            (new(2026, 12, 24), true),
            (new(2027, 2, 9), true),
            (new(2027, 2, 10), false),
            (new(2027, 3, 25), false),
            (new(2027, 3, 26), false),
            (new(2027, 3, 27), false),
            (new(2027, 3, 28), true),
            (new(2027, 5, 16), true),
            (new(2027, 5, 17), true),
            (new(2028, 3, 1), false),
            (new(2028, 4, 13), false),
            (new(2028, 4, 15), false),
            (new(2028, 4, 16), true),
        };
        foreach (var item in cases)
        {
            foreach (var hour in Enum.GetValues<Hour>())
            {
                var office = hours.Compose(item.Date, hour);
                var opening = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Versicle);
                var text = hour == Hour.MorningPrayer ? opening.IntroText : opening.Text;
                Assert.NotNull(text);
                var expected = OfficeData.Text("invitation") + (item.Alleluia ? "\n\nAlleluia." : string.Empty);
                Assert.Equal(expected, text);
                Assert.DoesNotContain("Outside Lent", text);
                Assert.DoesNotContain("{", text);
                if (hour == Hour.MorningPrayer)
                {
                    Assert.Equal(OfficeData.Text("invitatory-verse"), opening.Text);
                    Assert.Equal(4, opening.Options!.Count);
                }
            }
        }
    }

    private sealed class NoTextProvider : IHoursTextProvider
    {
        public bool IsSample => false;

        public string Acknowledgment => string.Empty;

        public LicensedText? Find(IEnumerable<string> keys) => null;
    }
}
