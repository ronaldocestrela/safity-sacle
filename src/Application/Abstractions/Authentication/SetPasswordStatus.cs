namespace SafetyScale.Application.Abstractions.Authentication;

public enum SetPasswordStatus
{
    Success,
    InvalidToken,
    UserNotFound,
    InvalidPassword,
}
