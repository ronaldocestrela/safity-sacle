using SafetyScale.Application.Sectors.Common;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Application.SecurityGuards.Common;

public sealed record SecurityGuardDto(
    Guid Id,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<SectorDto> Sectors);

public static class SecurityGuardMappings
{
    public static SecurityGuardDto ToDto(this SecurityGuard securityGuard)
    {
        var sectors = securityGuard.SecurityGuardSectors?
            .Select(x => x.Sector)
            .Where(s => s is not null)
            .Cast<Sector>()
            .OrderBy(s => s.Name)
            .Select(x => x.ToSummaryDto())
            .ToList() ?? [];

        return new SecurityGuardDto(
            securityGuard.Id,
            securityGuard.Name,
            securityGuard.IsActive,
            securityGuard.CreatedAt,
            sectors);
    }
}
