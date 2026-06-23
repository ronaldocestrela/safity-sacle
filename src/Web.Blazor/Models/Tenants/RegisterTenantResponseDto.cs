namespace SafetyScale.Web.Blazor.Models.Tenants;

/// <summary>Parity with <c>SafetyScale.Api.Contracts.Tenants.RegisterTenantResponse</c>.</summary>
public sealed record RegisterTenantResponseDto(Guid TenantId, string AdminUserId, string TenantSlug);
