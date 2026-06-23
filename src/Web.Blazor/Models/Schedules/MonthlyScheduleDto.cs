namespace SafetyScale.Web.Blazor.Models.Schedules;

/// <summary>Parity with <c>SafetyScale.Application.Schedules.Common.MonthlyScheduleDto</c>.</summary>
public sealed record MonthlyScheduleDto(
    Guid Id,
    int Month,
    int Year,
    DateTime GeneratedAt,
    IReadOnlyList<ScheduleItemDto> Items);
