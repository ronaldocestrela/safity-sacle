namespace SafetyScale.Web.Blazor.Models.Tenants;

/// <summary>Parity with <c>SafetyScale.Api.Contracts.Tenants.RegisterTenantRequest</c>.</summary>
public sealed record RegisterTenantRequestDto(
    string TenantName,
    string AdminName,
    string AdminEmail,
    string AdminPassword,
    string ConfirmPassword);
