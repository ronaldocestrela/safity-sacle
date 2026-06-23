namespace SafetyScale.Web.Blazor.Models.Sectors;

/// <summary>Parity with <c>SafetyScale.Application.Sectors.Common.SectorDto</c>.</summary>
public sealed record SectorDto(
    Guid Id,
    string Name,
    string? Description,
    int RequiredGuardsPerDay,
    bool IsActive,
    DateTime CreatedAt);
