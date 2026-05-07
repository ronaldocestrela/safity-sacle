using FluentAssertions;
using SafetyScale.Domain.Services;

namespace SafetyScale.Tests.Domain.Services;

public class ScheduleGeneratorServiceTests
{
    private readonly ScheduleGeneratorService _sut = new();

    [Fact]
    public void Generate_ShouldFail_WhenNoActiveGuards()
    {
        var result = _sut.Generate(6, 2030, [], new Dictionary<Guid, HashSet<DateOnly>>());

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be(ScheduleGenerationFailureReason.NoActiveGuards);
    }

    [Fact]
    public void Generate_ShouldNeverAssignUnavailableGuardOnThatDay()
    {
        var g1 = Guid.NewGuid();
        var g2 = Guid.NewGuid();
        var unavailableSaturday = new DateOnly(2035, 3, 2); // Saturday

        var unavailable = new Dictionary<Guid, HashSet<DateOnly>>
        {
            [g1] = new HashSet<DateOnly> { unavailableSaturday },
        };

        var result = _sut.Generate(3, 2035, new[] { g1, g2 }.OrderBy(x => x).ToList(), unavailable);

        result.Success.Should().BeTrue();
        result.Schedule!.Items.Should().ContainSingle(x => x.Date == unavailableSaturday && x.SecurityGuardId == g2);
    }

    [Fact]
    public void Generate_ShouldBalanceWeekendShifts_WhenThreeGuardsAndNoUnavailable()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }.OrderBy(x => x).ToList();
        var result = _sut.Generate(7, 2035, ids, new Dictionary<Guid, HashSet<DateOnly>>());

        result.Success.Should().BeTrue();

        var weekendCounts = ids.ToDictionary(id => id, _ => 0);
        foreach (var item in result.Schedule!.Items.Where(i => i.IsWeekend))
        {
            weekendCounts[item.SecurityGuardId]++;
        }

        var min = weekendCounts.Values.Min();
        var max = weekendCounts.Values.Max();
        (max - min).Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public void Generate_ShouldFail_WhenEveryGuardUnavailableOnSameDay()
    {
        var g1 = Guid.NewGuid();
        var g2 = Guid.NewGuid();
        var day = new DateOnly(2035, 5, 10);

        var unavailable = new Dictionary<Guid, HashSet<DateOnly>>
        {
            [g1] = new HashSet<DateOnly> { day },
            [g2] = new HashSet<DateOnly> { day },
        };

        var result = _sut.Generate(5, 2035, new[] { g1, g2 }.OrderBy(x => x).ToList(), unavailable);

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be(ScheduleGenerationFailureReason.CouldNotCoverDay);
        result.FailedDate.Should().Be(day);
    }

    [Fact]
    public void Generate_ShouldCoverEveryDayInMonth_WhenSolvable()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() }.OrderBy(x => x).ToList();
        var result = _sut.Generate(2, 2036, ids, new Dictionary<Guid, HashSet<DateOnly>>());

        result.Success.Should().BeTrue();
        var daysInMonth = DateTime.DaysInMonth(2036, 2);
        result.Schedule!.Items.Should().HaveCount(daysInMonth);
        result.Schedule.Items.Select(i => i.Date).Distinct().Should().HaveCount(daysInMonth);
    }
}
