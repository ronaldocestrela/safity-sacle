namespace SafetyScale.Web.Blazor.Models.SecurityGuards;

/// <summary>Parity with <c>SafetyScale.Api.Contracts.SecurityGuards.UpdateSecurityGuardSectorsRequest</c>.</summary>
public sealed record SetSecurityGuardSectorsRequestDto(IReadOnlyList<Guid> SectorIds);
