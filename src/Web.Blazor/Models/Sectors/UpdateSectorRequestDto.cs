namespace SafetyScale.Web.Blazor.Models.Sectors;

/// <summary>Parity with <c>SafetyScale.Api.Contracts.Sectors.UpdateSectorRequest</c>.</summary>
public sealed record UpdateSectorRequestDto(
    string Name,
    string? Description = null,
    int RequiredGuardsPerDay = 1);
