namespace SafetyScale.Web.Blazor.Models.UnavailableDays;

/// <summary>Parity with <c>SafetyScale.Api.Contracts.UnavailableDays.AddUnavailableDayRequest</c>.</summary>
public sealed record AddUnavailableDayRequestDto(DateOnly Date, string? Reason);
