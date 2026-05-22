using FluentAssertions;
using SafetyScale.Domain.Services;

namespace SafetyScale.Tests.Domain.Services;

public class ScheduleGeneratorServiceTests
{
    private readonly ScheduleGeneratorService _sut = new();

    private static SectorWorkloadDefinition Work(Guid sectorId, int requiredPerDay, IReadOnlyList<Guid> guardIdsOrdered)
        => new(sectorId, requiredPerDay, guardIdsOrdered);

    private static SectorWorkloadDefinition Work(Guid sectorId, int requiredPerDay, params Guid[] guardIdsOrdered)
        => new(sectorId, requiredPerDay, guardIdsOrdered);

    [Fact]
    public void Generate_ShouldFail_WhenWorkloadSectorsMissing()
    {
        var result = _sut.Generate(
            6,
            2030,
            Array.Empty<SectorWorkloadDefinition>(),
            new Dictionary<Guid, HashSet<DateOnly>>());

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be(ScheduleGenerationFailureReason.NoConfiguredSectors);
    }

    [Fact]
    public void Generate_ShouldNeverAssignUnavailableGuardOnThatDay()
    {
        var sectorId = Guid.NewGuid();
        var g1 = Guid.NewGuid();
        var g2 = Guid.NewGuid();
        var unavailableSaturday = new DateOnly(2035, 3, 2); // Saturday

        var unavailable = new Dictionary<Guid, HashSet<DateOnly>>
        {
            [g1] = new HashSet<DateOnly> { unavailableSaturday },
        };

        var result = _sut.Generate(
            3,
            2035,
            new List<SectorWorkloadDefinition> { Work(sectorId, 1, new[] { g1, g2 }.OrderBy(x => x).ToArray()) },
            unavailable);

        result.Success.Should().BeTrue();
        result.Schedule!.Items.Should().ContainSingle(x => x.Date == unavailableSaturday && x.SecurityGuardId == g2 && x.SectorId == sectorId);
    }

    [Fact]
    public void Generate_ShouldBalanceWeekendShifts_WhenThreeGuardsAndNoUnavailable()
    {
        var sectorId = Guid.NewGuid();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }.OrderBy(x => x).ToList();
        var result = _sut.Generate(
            7,
            2035,
            new List<SectorWorkloadDefinition> { Work(sectorId, 1, ids) },
            new Dictionary<Guid, HashSet<DateOnly>>());

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
        var sectorId = Guid.NewGuid();
        var g1 = Guid.NewGuid();
        var g2 = Guid.NewGuid();
        var day = new DateOnly(2035, 5, 10);

        var unavailable = new Dictionary<Guid, HashSet<DateOnly>>
        {
            [g1] = new HashSet<DateOnly> { day },
            [g2] = new HashSet<DateOnly> { day },
        };

        var result = _sut.Generate(
            5,
            2035,
            new List<SectorWorkloadDefinition> { Work(sectorId, 1, new[] { g1, g2 }.OrderBy(x => x).ToArray()) },
            unavailable);

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be(ScheduleGenerationFailureReason.CouldNotCoverDay);
        result.FailedDate.Should().Be(day);
    }

    [Fact]
    public void Generate_ShouldCoverEverySectorPositionInMonth_WhenSolvableSingleSector()
    {
        var sectorId = Guid.NewGuid();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() }.OrderBy(x => x).ToList();
        var result = _sut.Generate(
            2,
            2036,
            new List<SectorWorkloadDefinition> { Work(sectorId, 1, ids) },
            new Dictionary<Guid, HashSet<DateOnly>>());

        result.Success.Should().BeTrue();
        var daysInMonth = DateTime.DaysInMonth(2036, 2);
        result.Schedule!.Items.Should().HaveCount(daysInMonth);
        result.Schedule.Items.Should().OnlyContain(i => i.SectorId == sectorId);
        result.Schedule.Items.Select(i => i.Date).Distinct().Should().HaveCount(daysInMonth);
        result.Schedule.Items.GroupBy(i => i.Date).Should().OnlyContain(g => g.Count() == 1);
    }

    [Fact]
    public void Generate_ShouldRespectEligiblePool_PerSector_PerDay()
    {
        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();
        var ga = Guid.NewGuid();
        var gb = Guid.NewGuid();
        var gc = Guid.NewGuid();

        var result = _sut.Generate(
            1,
            2044,
            new List<SectorWorkloadDefinition>
            {
                Work(s1, 1, ga, gb),
                Work(s2, 1, gb, gc),
            },
            new Dictionary<Guid, HashSet<DateOnly>>());

        result.Success.Should().BeTrue();

        foreach (var day in result.Schedule!.Items.GroupBy(i => i.Date))
        {
            day.Should().HaveCount(2, because: "two sectors × one position/day");
            var bySector = day.ToLookup(i => i.SectorId);
            bySector[s1].Should().ContainSingle(i => i.SecurityGuardId == ga || i.SecurityGuardId == gb);
            bySector[s2].Should().ContainSingle(i => i.SecurityGuardId == gb || i.SecurityGuardId == gc);

            day.Select(i => i.SecurityGuardId).Distinct().Should().HaveCount(2, because: "no reuse same day");
        }
    }

    [Fact]
    public void Generate_ShouldFail_WhenInsufficientDistinctGuards_OnSameCalendarDay()
    {
        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();
        var ga = Guid.NewGuid();
        var gb = Guid.NewGuid();

        // Two sectors need 2 posts each (=4) but each sector only exposes 2 guards (same overlapping pool of 2)
        var result = _sut.Generate(
            1,
            2045,
            new List<SectorWorkloadDefinition>
            {
                Work(s1, 2, ga, gb),
                Work(s2, 2, ga, gb),
            },
            new Dictionary<Guid, HashSet<DateOnly>>());

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be(ScheduleGenerationFailureReason.CouldNotCoverDay);
        result.FailedDate.Should().NotBeNull();
        result.FailedDate!.Value.Year.Should().Be(2045);
        result.FailedDate.Value.Month.Should().Be(1);
        (result.FailedDate.Value.DayOfWeek == DayOfWeek.Saturday ||
         result.FailedDate.Value.DayOfWeek == DayOfWeek.Sunday).Should().BeTrue();
    }
}
