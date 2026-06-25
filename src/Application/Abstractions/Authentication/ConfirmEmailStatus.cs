namespace SafetyScale.Application.Abstractions.Authentication;

public enum ConfirmEmailStatus
{
    Success,
    AlreadyConfirmed,
    InvalidToken,
    UserNotFound
}
