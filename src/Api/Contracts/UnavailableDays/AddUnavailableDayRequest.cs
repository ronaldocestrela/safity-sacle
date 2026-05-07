namespace SafetyScale.Api.Contracts.UnavailableDays;

public sealed record AddUnavailableDayRequest(DateOnly Date, string? Reason);
