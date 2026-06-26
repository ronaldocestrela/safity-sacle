namespace SafetyScale.Web.Blazor.Models.SecurityGuards;

/// <summary>Parity with <c>SafetyScale.Api.Contracts.SecurityGuards.CreateSecurityGuardRequest</c>.</summary>
public sealed record CreateSecurityGuardRequestDto(string Name, string Email);
