using SafetyScale.Web.Blazor.Models.Sectors;

namespace SafetyScale.Web.Blazor.Models.SecurityGuards;

/// <summary>Parity with <c>SafetyScale.Application.SecurityGuards.Common.SecurityGuardDto</c>.</summary>
public sealed record SecurityGuardDto(
    Guid Id,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<SectorDto> Sectors);
