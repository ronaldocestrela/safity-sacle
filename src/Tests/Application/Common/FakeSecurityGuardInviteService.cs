using SafetyScale.Application.Abstractions.Authentication;

namespace SafetyScale.Tests.Application.Common;

public sealed class FakeSecurityGuardInviteService : ISecurityGuardInviteService
{
    public bool EmailAvailable { get; init; } = true;

    public List<(Guid GuardId, string Email, string DisplayName)> Invites { get; } = [];

    public Task<bool> IsEmailAvailableAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(EmailAvailable);

    public Task InviteAsync(
        Guid securityGuardId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        Invites.Add((securityGuardId, email, displayName));
        return Task.CompletedTask;
    }
}
