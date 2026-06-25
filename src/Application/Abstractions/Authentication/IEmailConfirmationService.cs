namespace SafetyScale.Application.Abstractions.Authentication;

public interface IEmailConfirmationService
{
    Task EnqueueConfirmationEmailAsync(
        string userId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default);

    Task<ConfirmEmailResult> ConfirmAsync(
        string userId,
        string token,
        CancellationToken cancellationToken = default);
}
