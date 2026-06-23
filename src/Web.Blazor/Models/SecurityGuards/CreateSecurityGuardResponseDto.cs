namespace SafetyScale.Web.Blazor.Models.SecurityGuards;

/// <summary>Response body from <c>POST /api/security-guards</c> (201 Created).</summary>
public sealed record CreateSecurityGuardResponseDto(Guid Id);
