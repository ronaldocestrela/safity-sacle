namespace SafetyScale.Api.Contracts.Sectors;

public sealed record UpdateSectorRequest(string Name, string? Description = null);
