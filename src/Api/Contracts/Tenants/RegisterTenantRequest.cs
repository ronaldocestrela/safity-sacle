namespace SafetyScale.Api.Contracts.Tenants;

public sealed record RegisterTenantRequest(
    string TenantName,
    string AdminName,
    string AdminEmail,
    string AdminPassword,
    string ConfirmPassword);
