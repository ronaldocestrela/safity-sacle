namespace SafetyScale.Application.Abstractions.Tenancy;

public sealed record RegisterTenantResult(
    RegisterTenantStatus Status,
    Guid? TenantId = null,
    string? AdminUserId = null,
    string? TenantSlug = null,
    IReadOnlyList<string>? Errors = null);
