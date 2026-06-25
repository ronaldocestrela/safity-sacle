namespace SafetyScale.Web.Blazor.Services.Auth;

public enum ConfirmEmailOutcomeStatus
{
    Success,
    AlreadyConfirmed,
    InvalidToken,
    UserNotFound,
    Network,
}

public sealed record ConfirmEmailOutcome(
    ConfirmEmailOutcomeStatus Status,
    string? Message = null);
