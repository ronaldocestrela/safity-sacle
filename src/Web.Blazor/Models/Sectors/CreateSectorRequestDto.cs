namespace SafetyScale.Web.Blazor.Models.Sectors;

/// <summary>Parity with <c>SafetyScale.Api.Contracts.Sectors.CreateSectorRequest</c>.</summary>
public sealed record CreateSectorRequestDto(
    string Name,
    string? Description = null,
    int RequiredGuardsPerDay = 1);
