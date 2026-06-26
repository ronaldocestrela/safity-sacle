namespace SafetyScale.Application.Abstractions.Authentication;

public interface ISecurityGuardInviteService
{
    Task<bool> IsEmailAvailableAsync(string email, CancellationToken cancellationToken = default);

    Task InviteAsync(
        Guid securityGuardId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default);
}
