namespace SafetyScale.Application.Abstractions.Authentication;

public sealed record SetPasswordResult(
    SetPasswordStatus Status,
    IReadOnlyList<string>? Errors = null);
