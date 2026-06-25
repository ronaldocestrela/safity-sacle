namespace SafetyScale.Api.Contracts.Platform;

public sealed record CreatePlatformTenantRequest(
    string TenantName,
    string AdminName,
    string AdminEmail,
    string AdminPassword);

public sealed record PlatformTenantResponse(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    DateTime CreatedAt);

public sealed record CreatePlatformTenantResponse(
    Guid TenantId,
    string AdminUserId,
    string TenantSlug);
