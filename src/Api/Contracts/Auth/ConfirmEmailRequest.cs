namespace SafetyScale.Api.Contracts.Auth;

public sealed record ConfirmEmailRequest(string UserId, string Token);
