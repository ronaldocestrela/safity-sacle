namespace SafetyScale.Web.Blazor.Models.Auth;

public sealed record SetPasswordRequestDto(string UserId, string Token, string Password);

public enum SetPasswordOutcomeStatus
{
    Success,
    InvalidPassword,
    InvalidToken,
    UserNotFound,
    Network,
}

public sealed record SetPasswordOutcome(SetPasswordOutcomeStatus Status, string? Message = null, IReadOnlyList<string>? Errors = null);
