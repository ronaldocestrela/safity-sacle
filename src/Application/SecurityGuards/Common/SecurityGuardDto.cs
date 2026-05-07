using SafetyScale.Domain.Entities;

namespace SafetyScale.Application.SecurityGuards.Common;

public sealed record SecurityGuardDto(Guid Id, string Name, bool IsActive, DateTime CreatedAt);

public static class SecurityGuardMappings
{
    public static SecurityGuardDto ToDto(this SecurityGuard securityGuard)
        => new(securityGuard.Id, securityGuard.Name, securityGuard.IsActive, securityGuard.CreatedAt);
}
