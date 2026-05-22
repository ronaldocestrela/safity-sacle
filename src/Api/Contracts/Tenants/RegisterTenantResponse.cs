namespace SafetyScale.Api.Contracts.Tenants;

public sealed record RegisterTenantResponse(Guid TenantId, string AdminUserId, string TenantSlug);
