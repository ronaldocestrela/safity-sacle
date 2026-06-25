namespace SafetyScale.Application.Abstractions.Tenancy;

public sealed record PlatformTenantSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    DateTime CreatedAt);

public sealed record CreatePlatformTenantInput(
    string TenantName,
    string AdminName,
    string AdminEmail,
    string AdminPassword);

public enum CreatePlatformTenantStatus
{
    Success,
    ValidationFailed,
    AdminEmailAlreadyExists,
    TenantSlugConflict,
    InvalidPassword,
}

public sealed record CreatePlatformTenantResult(
    CreatePlatformTenantStatus Status,
    Guid? TenantId = null,
    string? AdminUserId = null,
    string? TenantSlug = null,
    IReadOnlyList<string>? Errors = null);

public enum SetTenantActiveStatus
{
    Success,
    NotFound,
}

public sealed record SetTenantActiveResult(SetTenantActiveStatus Status);

public interface IPlatformTenantService
{
    Task<IReadOnlyList<PlatformTenantSummaryDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<CreatePlatformTenantResult> CreateAsync(
        CreatePlatformTenantInput input,
        CancellationToken cancellationToken = default);

    Task<SetTenantActiveResult> SetActiveAsync(
        Guid tenantId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
