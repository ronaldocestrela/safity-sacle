using System.Net.Http.Json;
using SafetyScale.Web.Blazor.Models.Platform;

namespace SafetyScale.Web.Blazor.Services.Api;

public sealed class PlatformTenantsApiClient(ApiHttpClient apiClient)
{
    public async Task<IReadOnlyList<PlatformTenantDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var response = await apiClient.GetAsync("/api/platform/tenants", cancellationToken: cancellationToken);
        response.EnsureSuccessStatusCode();
        return await apiClient.ReadJsonAsync<IReadOnlyList<PlatformTenantDto>>(response, cancellationToken)
               ?? Array.Empty<PlatformTenantDto>();
    }

    public async Task<bool> ActivateAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        using var response = await apiClient.PatchAsync(
            $"/api/platform/tenants/{tenantId}/activate",
            content: null,
            cancellationToken: cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeactivateAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        using var response = await apiClient.PatchAsync(
            $"/api/platform/tenants/{tenantId}/deactivate",
            content: null,
            cancellationToken: cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateCommercialAsync(
        Guid tenantId,
        UpdateTenantCommercialRequestDto request,
        CancellationToken cancellationToken = default)
    {
        using var response = await apiClient.PatchAsync(
            $"/api/platform/tenants/{tenantId}/commercial",
            JsonContent.Create(request, options: apiClient.JsonOptions),
            cancellationToken: cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<CreatePlatformTenantOutcome> CreateAsync(
        CreatePlatformTenantRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await apiClient.PostJsonAsync(
                "/api/platform/tenants",
                request,
                cancellationToken: cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                return CreatePlatformTenantOutcome.Success();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var message = await ApiErrorReader.ReadMessageAsync(response, cancellationToken);
                return CreatePlatformTenantOutcome.Fail(message ?? "Conflito ao criar tenant.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var message = await ApiErrorReader.ReadMessageAsync(response, cancellationToken);
                return CreatePlatformTenantOutcome.Fail(message ?? "Dados inválidos.");
            }

            return CreatePlatformTenantOutcome.Fail("Não foi possível conectar à API.");
        }
        catch (HttpRequestException)
        {
            return CreatePlatformTenantOutcome.Fail("Não foi possível conectar à API.");
        }
    }
}

public sealed record CreatePlatformTenantOutcome(bool Ok, string? Message = null)
{
    public static CreatePlatformTenantOutcome Success() => new(true);

    public static CreatePlatformTenantOutcome Fail(string message) => new(false, message);
}
