namespace SafetyScale.Application.Abstractions.Authentication;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }

    string? UserId { get; }

    IReadOnlyList<string> Roles { get; }

    Guid? SecurityGuardId { get; }

    bool IsInRole(string role);
}
