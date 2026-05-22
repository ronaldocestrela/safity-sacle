namespace SafetyScale.Api.Contracts.SecurityGuards;

public sealed record UpdateSecurityGuardSectorsRequest(IReadOnlyList<Guid> SectorIds);
