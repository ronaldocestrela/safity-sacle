using SafetyScale.Domain.Entities;

namespace SafetyScale.Domain.Services;

/// <summary>
/// Greedy generator: fills all sector/day positions without reusing guards on the same day.
/// Days are processed weekends first, then weekdays.
/// Tie-break within a sector slot: fewer weekend shifts, fewer total shifts, larger gap since last shift.
/// </summary>
public sealed class ScheduleGeneratorService
{
    public ScheduleGenerationResult Generate(
        int month,
        int year,
        IReadOnlyList<SectorWorkloadDefinition> sectors,
        IReadOnlyDictionary<Guid, HashSet<DateOnly>> unavailableByGuard)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);

        if (sectors.Count == 0 ||
            sectors.Sum(x => Math.Max(0, x.RequiredGuardsPerDay)) == 0)
        {
            return ScheduleGenerationResult.Fail(ScheduleGenerationFailureReason.NoConfiguredSectors, null);
        }

        var defsBySector = sectors.ToDictionary(d => d.SectorId);

        var allGuardIds = sectors
            .SelectMany(x => x.EligibleGuardIdsOrdered)
            .Distinct()
            .ToList();

        if (allGuardIds.Count == 0)
        {
            return ScheduleGenerationResult.Fail(ScheduleGenerationFailureReason.NoEligibleGuardsForSectors, null);
        }

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var weekendDates = new List<DateOnly>();
        var weekdayDates = new List<DateOnly>();

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                weekendDates.Add(date);
            }
            else
            {
                weekdayDates.Add(date);
            }
        }

        var assignmentOrder = new List<DateOnly>(weekendDates.Count + weekdayDates.Count);
        assignmentOrder.AddRange(weekendDates);
        assignmentOrder.AddRange(weekdayDates);

        var weekendCount = allGuardIds.ToDictionary(id => id, _ => 0);
        var totalCount = allGuardIds.ToDictionary(id => id, _ => 0);
        var lastAssigned = new Dictionary<Guid, DateOnly>();

        var scheduleId = Guid.NewGuid();
        var generatedAt = DateTime.UtcNow;
        var items = new List<ScheduleItem>();

        var orderedWorkload = sectors.Where(s => s.RequiredGuardsPerDay > 0).OrderBy(s => s.SectorId).ToList();

        foreach (var date in assignmentOrder)
        {
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var usedToday = new HashSet<Guid>();

            foreach (var slot in ExpandDailySlots(orderedWorkload))
            {
                var definition = defsBySector[slot];
                var eligible = definition
                    .EligibleGuardIdsOrdered
                    .Where(id =>
                    {
                        if (usedToday.Contains(id))
                        {
                            return false;
                        }

                        if (unavailableByGuard.TryGetValue(id, out var blocked))
                        {
                            return !blocked.Contains(date);
                        }

                        return true;
                    })
                    .ToList();

                if (eligible.Count == 0)
                {
                    return ScheduleGenerationResult.Fail(ScheduleGenerationFailureReason.CouldNotCoverDay, date);
                }

                var guardId = PickGuard(eligible, weekendCount, totalCount, lastAssigned, date);

                items.Add(new ScheduleItem
                {
                    Id = Guid.NewGuid(),
                    MonthlyScheduleId = scheduleId,
                    SecurityGuardId = guardId,
                    SectorId = slot,
                    Date = date,
                    IsWeekend = isWeekend,
                });

                usedToday.Add(guardId);

                totalCount[guardId]++;
                if (isWeekend)
                {
                    weekendCount[guardId]++;
                }

                lastAssigned[guardId] = date;
            }
        }

        var schedule = new MonthlySchedule
        {
            Id = scheduleId,
            Month = month,
            Year = year,
            GeneratedAt = generatedAt,
            Items = items,
        };

        return ScheduleGenerationResult.Ok(schedule);
    }

    /// <summary>Deterministic repeating pattern: all sectors ascending, RequiredGuardsPerDay times each.</summary>
    private static IEnumerable<Guid> ExpandDailySlots(IReadOnlyList<SectorWorkloadDefinition> orderedWorkload)
    {
        foreach (var def in orderedWorkload)
        {
            for (var i = 0; i < def.RequiredGuardsPerDay; i++)
            {
                yield return def.SectorId;
            }
        }
    }

    private static Guid PickGuard(
        IReadOnlyList<Guid> eligible,
        Dictionary<Guid, int> weekendCount,
        Dictionary<Guid, int> totalCount,
        Dictionary<Guid, DateOnly> lastAssigned,
        DateOnly date)
        => eligible
            .OrderBy(id => weekendCount[id])
            .ThenBy(id => totalCount[id])
            .ThenByDescending(id => GapDays(lastAssigned, id, date))
            .ThenBy(id => id)
            .First();

    private static int GapDays(Dictionary<Guid, DateOnly> lastAssigned, Guid guardId, DateOnly currentDate)
    {
        if (!lastAssigned.TryGetValue(guardId, out var last))
        {
            return 10_000;
        }

        return currentDate.DayNumber - last.DayNumber;
    }
}
