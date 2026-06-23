namespace SafetyScale.Web.Blazor.Models.UnavailableDays;

/// <summary>Parity with <c>SafetyScale.Application.UnavailableDays.Common.UnavailableDayDto</c>.</summary>
public sealed record UnavailableDayDto(Guid Id, Guid SecurityGuardId, DateOnly Date, string? Reason);
