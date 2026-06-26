namespace SafetyScale.Api.Contracts.Auth;

public sealed record SetPasswordRequest(string UserId, string Token, string Password);
