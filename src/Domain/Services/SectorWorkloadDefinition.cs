namespace SafetyScale.Domain.Services;

/// <summary>
/// Active sector staffing requirements plus eligible guards (assigned to sector, active roster).
/// </summary>
public sealed record SectorWorkloadDefinition(
    Guid SectorId,
    int RequiredGuardsPerDay,
    IReadOnlyList<Guid> EligibleGuardIdsOrdered);
