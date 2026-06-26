namespace SafetyScale.Application.Abstractions.Tenancy;

public sealed record RegisterTenantInput(
    string TenantName,
    string AdminName,
    string AdminEmail,
    string AdminPassword,
    string ConfirmPassword,
    Guid? PlatformPlanId = null,
    LeadStatusDto LeadStatus = LeadStatusDto.New);
