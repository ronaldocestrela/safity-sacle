namespace SafetyScale.Application.Abstractions.Tenancy;

public sealed record PlatformTenantSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    DateTime CreatedAt,
    LeadStatusDto LeadStatus,
    Guid? PlatformPlanId,
    string? PlatformPlanName);

public enum LeadStatusDto
{
    New = 0,
    Contacted = 1,
    ProposalSent = 2,
    Contracted = 3,
    Lost = 4,
}

public sealed record CreatePlatformTenantInput(
    string TenantName,
    string AdminName,
    string AdminEmail,
    string AdminPassword,
    Guid? PlatformPlanId = null,
    LeadStatusDto LeadStatus = LeadStatusDto.New);

public enum CreatePlatformTenantStatus
{
    Success,
    ValidationFailed,
    AdminEmailAlreadyExists,
    TenantSlugConflict,
    InvalidPassword,
    PlanNotFound,
    PlanInactive,
    ContractedRequiresPlan,
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

public sealed record UpdateTenantCommercialInput(
    Guid? PlatformPlanId,
    LeadStatusDto LeadStatus);

public enum UpdateTenantCommercialStatus
{
    Success,
    NotFound,
    ValidationFailed,
    PlanNotFound,
    PlanInactive,
    ContractedRequiresPlan,
    PlanDowngradeNotAllowed,
}

public sealed record UpdateTenantCommercialResult(
    UpdateTenantCommercialStatus Status,
    IReadOnlyList<string>? Errors = null);

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

    Task<UpdateTenantCommercialResult> UpdateCommercialAsync(
        Guid tenantId,
        UpdateTenantCommercialInput input,
        CancellationToken cancellationToken = default);
}
