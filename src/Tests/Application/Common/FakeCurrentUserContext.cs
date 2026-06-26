namespace SafetyScale.Tests.Application.Common;

using SafetyScale.Application.Abstractions.Authentication;

public sealed class FakeCurrentUserContext : ICurrentUserContext
{
    public bool IsAuthenticated { get; init; } = true;

    public string? UserId { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = [];

    public Guid? SecurityGuardId { get; init; }

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.Ordinal);

    public static FakeCurrentUserContext Unrestricted { get; } = new();

    public static FakeCurrentUserContext ForSecurityGuard(Guid securityGuardId) =>
        new()
        {
            Roles = ["SecurityGuard"],
            SecurityGuardId = securityGuardId,
        };
}
