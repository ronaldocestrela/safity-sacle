namespace SafetyScale.Application.Abstractions.Authentication;

public sealed record LoginResult(LoginResultStatus Status, string? Token = null);
