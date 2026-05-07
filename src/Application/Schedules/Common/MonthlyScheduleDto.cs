using SafetyScale.Domain.Entities;

namespace SafetyScale.Application.Schedules.Common;

public sealed record ScheduleItemDto(
    Guid Id,
    Guid SecurityGuardId,
    string SecurityGuardName,
    bool SecurityGuardIsActive,
    DateOnly Date,
    bool IsWeekend);

public sealed record MonthlyScheduleDto(
    Guid Id,
    int Month,
    int Year,
    DateTime GeneratedAt,
    IReadOnlyList<ScheduleItemDto> Items);

public static class MonthlyScheduleMappings
{
    public static MonthlyScheduleDto ToMonthlyScheduleDto(this MonthlySchedule monthlySchedule)
    {
        var items = monthlySchedule.Items
            .OrderBy(i => i.Date)
            .Select(ToScheduleItemDto)
            .ToList();

        return new MonthlyScheduleDto(
            monthlySchedule.Id,
            monthlySchedule.Month,
            monthlySchedule.Year,
            monthlySchedule.GeneratedAt,
            items);
    }

    private static ScheduleItemDto ToScheduleItemDto(ScheduleItem item)
    {
        var guard = item.SecurityGuard;
        return new ScheduleItemDto(
            item.Id,
            item.SecurityGuardId,
            guard?.Name ?? string.Empty,
            guard?.IsActive ?? false,
            item.Date,
            item.IsWeekend);
    }
}
