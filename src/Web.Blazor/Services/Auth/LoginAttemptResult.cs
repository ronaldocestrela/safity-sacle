namespace SafetyScale.Web.Blazor.Services.Auth;

public enum LoginFailureReason
{
    Invalid,
    EmailNotConfirmed,
    Network,
}

public sealed record LoginAttemptResult(bool Ok, LoginFailureReason? Reason = null);
