using System.Net.Http.Json;
using SafetyScale.Web.Blazor.Models.SecurityGuards;

namespace SafetyScale.Web.Blazor.Services.Api;

/// <summary>
/// Security guards API client. Parity with React <c>securityGuardsApi.ts</c>.
/// </summary>
public sealed class SecurityGuardsApiClient(ApiHttpClient apiClient)
{
    /// <summary>
    /// Lists guards. <paramref name="isActive"/> <c>null</c> returns all guards.
    /// </summary>
    public async Task<IReadOnlyList<SecurityGuardDto>> ListAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var path = isActive.HasValue
            ? $"/api/security-guards?isActive={isActive.Value.ToString().ToLowerInvariant()}"
            : "/api/security-guards";

        using var response = await apiClient.GetAsync(path, cancellationToken: cancellationToken);
        await ApiClientResponseHelper.EnsureOkAsync(
            response,
            "Não foi possível carregar seguranças.",
            cancellationToken);

        var list = await apiClient.ReadJsonAsync<List<SecurityGuardDto>>(response, cancellationToken);
        return list ?? [];
    }

    public async Task<Guid> CreateAsync(
        CreateSecurityGuardRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var body = request with { Name = request.Name.Trim() };

        using var response = await apiClient.PostJsonAsync(
            "/api/security-guards",
            body,
            cancellationToken: cancellationToken);

        await ApiClientResponseHelper.EnsureOkAsync(
            response,
            "Não foi possível criar segurança.",
            cancellationToken);

        var created = await apiClient.ReadJsonAsync<CreateSecurityGuardResponseDto>(response, cancellationToken);
        if (created is null || created.Id == Guid.Empty)
        {
            throw new ApiException((int)response.StatusCode, null, "Não foi possível criar segurança.");
        }

        return created.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        UpdateSecurityGuardRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var body = request with { Name = request.Name.Trim() };
        var path = $"/api/security-guards/{Uri.EscapeDataString(id.ToString())}";

        using var response = await apiClient.SendAsync(
            path,
            HttpMethod.Put,
            JsonContent.Create(body, options: apiClient.JsonOptions),
            cancellationToken: cancellationToken);

        await ApiClientResponseHelper.EnsureOkAsync(
            response,
            "Não foi possível salvar alterações.",
            cancellationToken);
    }

    public async Task InactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var path = $"/api/security-guards/{Uri.EscapeDataString(id.ToString())}/inactive";

        using var response = await apiClient.SendAsync(
            path,
            HttpMethod.Patch,
            cancellationToken: cancellationToken);

        await ApiClientResponseHelper.EnsureOkAsync(
            response,
            "Não foi possível inativar.",
            cancellationToken);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var path = $"/api/security-guards/{Uri.EscapeDataString(id.ToString())}/active";

        using var response = await apiClient.SendAsync(
            path,
            HttpMethod.Patch,
            cancellationToken: cancellationToken);

        await ApiClientResponseHelper.EnsureOkAsync(
            response,
            "Não foi possível reativar.",
            cancellationToken);
    }

    public async Task SetSectorsAsync(
        Guid id,
        SetSecurityGuardSectorsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var path = $"/api/security-guards/{Uri.EscapeDataString(id.ToString())}/sectors";

        using var response = await apiClient.SendAsync(
            path,
            HttpMethod.Put,
            JsonContent.Create(request, options: apiClient.JsonOptions),
            cancellationToken: cancellationToken);

        await ApiClientResponseHelper.EnsureOkAsync(
            response,
            "Não foi possível salvar setores do segurança.",
            cancellationToken);
    }
}
