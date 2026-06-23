namespace SafetyScale.Web.Blazor.Models.Schedules;

/// <summary>Parity with <c>SafetyScale.Application.Schedules.Common.ScheduleItemDto</c>.</summary>
public sealed record ScheduleItemDto(
    Guid Id,
    Guid SecurityGuardId,
    string SecurityGuardName,
    bool SecurityGuardIsActive,
    Guid SectorId,
    string SectorName,
    DateOnly Date,
    bool IsWeekend);
