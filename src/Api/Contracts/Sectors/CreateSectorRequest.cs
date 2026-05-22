namespace SafetyScale.Api.Contracts.Sectors;

public sealed record CreateSectorRequest(string Name, string? Description = null, int RequiredGuardsPerDay = 1);
