namespace SafetyScale.Web.Blazor.Models.Platform;

public sealed record PlatformTenantDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    DateTime CreatedAt,
    LeadStatusDto LeadStatus,
    Guid? PlatformPlanId,
    string? PlatformPlanName);

public sealed record CreatePlatformTenantRequestDto(
    string TenantName,
    string AdminName,
    string AdminEmail,
    string AdminPassword,
    Guid? PlatformPlanId = null,
    LeadStatusDto LeadStatus = LeadStatusDto.New);

public sealed record UpdateTenantCommercialRequestDto(
    Guid? PlatformPlanId,
    LeadStatusDto LeadStatus);

public enum LeadStatusDto
{
    New = 0,
    Contacted = 1,
    ProposalSent = 2,
    Contracted = 3,
    Lost = 4,
}

public static class LeadStatusLabels
{
    public static string GetLabel(LeadStatusDto status) => status switch
    {
        LeadStatusDto.New => "Novo",
        LeadStatusDto.Contacted => "Contatado",
        LeadStatusDto.ProposalSent => "Proposta enviada",
        LeadStatusDto.Contracted => "Contratado",
        LeadStatusDto.Lost => "Perdido",
        _ => status.ToString(),
    };
}
