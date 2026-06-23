namespace SafetyScale.Web.Blazor.Models.Schedules;

/// <summary>Parity with <c>SafetyScale.Api.Contracts.Schedules.GenerateMonthlyScheduleRequest</c>.</summary>
public sealed record GenerateMonthlyScheduleRequestDto(int Month, int Year);
