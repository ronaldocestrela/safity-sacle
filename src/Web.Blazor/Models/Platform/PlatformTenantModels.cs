namespace SafetyScale.Web.Blazor.Models.Platform;

public sealed record PlatformTenantDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    DateTime CreatedAt);

public sealed record CreatePlatformTenantRequestDto(
    string TenantName,
    string AdminName,
    string AdminEmail,
    string AdminPassword);
