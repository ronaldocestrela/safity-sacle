using FluentAssertions;
using SafetyScale.Web.Blazor.Services.Calendar;

namespace SafetyScale.Tests.Web.Blazor.Calendar;

public sealed class MonthGridTests
{
    [Fact]
    public void BuildMonthGrid_ForKnownMonth_StartsOnCorrectWeekdayAndContainsInMonthDays()
    {
        var cells = MonthGrid.BuildMonthGrid(2026, 5);

        cells.Should().HaveCount(c => c % 7 == 0);
        cells.Count(c => c.InMonth).Should().Be(30);
        cells.First(c => c.InMonth).Key.Should().Be("2026-06-01");
        cells.Last(c => c.InMonth).Key.Should().Be("2026-06-30");
    }

    [Fact]
    public void DateKeyFromLocal_FormatsZeroPaddedKeys()
    {
        MonthGrid.DateKeyFromLocal(2026, 3, 5).Should().Be("2026-03-05");
        MonthGrid.TodayKeyLocal().Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}$");
    }

    [Fact]
    public void BuildMonthGrid_PadsLeadingAndTrailingDays()
    {
        var cells = MonthGrid.BuildMonthGrid(2026, 4);

        cells.First().InMonth.Should().BeFalse();
        cells.Last().InMonth.Should().BeFalse();
        cells.Should().Contain(c => c.Key == "2026-05-01" && c.InMonth);
    }
}
