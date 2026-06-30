namespace SafetyScale.Api.Contracts.Platform;

public sealed record CreatePlatformTenantRequest(
    string TenantName,
    string AdminName,
    string AdminEmail,
    string AdminPassword,
    Guid? PlatformPlanId = null,
    LeadStatusContract LeadStatus = LeadStatusContract.New);

public sealed record PlatformTenantResponse(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    DateTime CreatedAt,
    LeadStatusContract LeadStatus,
    Guid? PlatformPlanId,
    string? PlatformPlanName);

public sealed record CreatePlatformTenantResponse(
    Guid TenantId,
    string AdminUserId,
    string TenantSlug);

public sealed record UpdateTenantCommercialRequest(
    Guid? PlatformPlanId,
    LeadStatusContract LeadStatus);

public enum LeadStatusContract
{
    New = 0,
    Contacted = 1,
    ProposalSent = 2,
    Contracted = 3,
    Lost = 4,
}
