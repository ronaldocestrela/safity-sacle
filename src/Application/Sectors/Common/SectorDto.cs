using SafetyScale.Domain.Entities;

namespace SafetyScale.Application.Sectors.Common;

public sealed record SectorDto(
    Guid Id,
    string Name,
    string? Description,
    int RequiredGuardsPerDay,
    bool IsActive,
    DateTime CreatedAt);

public static class SectorMappings
{
    public static SectorDto ToDto(this Sector sector)
        => new(
            sector.Id,
            sector.Name,
            sector.Description,
            sector.RequiredGuardsPerDay,
            sector.IsActive,
            sector.CreatedAt);

    /// <summary>Lightweight projection for nesting under security guards.</summary>
    public static SectorDto ToSummaryDto(this Sector sector)
        => new(sector.Id, sector.Name, null, sector.RequiredGuardsPerDay, sector.IsActive, sector.CreatedAt);
}
