using SafetyScale.Domain.Entities;

namespace SafetyScale.Domain.Services;

/// <summary>
/// Greedy generator: weekends first, then weekdays.
/// Tie-break: fewer weekend shifts, fewer total shifts, larger gap since last shift.
/// </summary>
public sealed class ScheduleGeneratorService
{
    public ScheduleGenerationResult Generate(
        int month,
        int year,
        IReadOnlyList<Guid> activeGuardIds,
        IReadOnlyDictionary<Guid, HashSet<DateOnly>> unavailableByGuard)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);

        if (activeGuardIds.Count == 0)
        {
            return ScheduleGenerationResult.Fail(ScheduleGenerationFailureReason.NoActiveGuards, null);
        }

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var weekendDates = new List<DateOnly>();
        var weekdayDates = new List<DateOnly>();

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            var dow = date.DayOfWeek;
            if (dow is DayOfWeek.Saturday or DayOfWeek.Sunday)
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

        var weekendCount = activeGuardIds.ToDictionary(id => id, _ => 0);
        var totalCount = activeGuardIds.ToDictionary(id => id, _ => 0);
        var lastAssigned = new Dictionary<Guid, DateOnly>();

        var scheduleId = Guid.NewGuid();
        var generatedAt = DateTime.UtcNow;
        var items = new List<ScheduleItem>();

        foreach (var date in assignmentOrder)
        {
            var eligible = activeGuardIds
                .Where(id =>
                {
                    if (unavailableByGuard.TryGetValue(id, out var set))
                    {
                        return !set.Contains(date);
                    }

                    return true;
                })
                .ToList();

            if (eligible.Count == 0)
            {
                return ScheduleGenerationResult.Fail(ScheduleGenerationFailureReason.CouldNotCoverDay, date);
            }

            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            Guid PickGuard()
            {
                return eligible
                    .OrderBy(id => weekendCount[id])
                    .ThenBy(id => totalCount[id])
                    .ThenByDescending(id => GapDays(lastAssigned, id, date))
                    .ThenBy(id => id)
                    .First();
            }

            var guardId = PickGuard();

            var item = new ScheduleItem
            {
                Id = Guid.NewGuid(),
                MonthlyScheduleId = scheduleId,
                SecurityGuardId = guardId,
                Date = date,
                IsWeekend = isWeekend,
            };
            items.Add(item);

            totalCount[guardId]++;
            if (isWeekend)
            {
                weekendCount[guardId]++;
            }

            lastAssigned[guardId] = date;
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

    private static int GapDays(Dictionary<Guid, DateOnly> lastAssigned, Guid guardId, DateOnly currentDate)
    {
        if (!lastAssigned.TryGetValue(guardId, out var last))
        {
            return 10_000;
        }

        return currentDate.DayNumber - last.DayNumber;
    }
}
