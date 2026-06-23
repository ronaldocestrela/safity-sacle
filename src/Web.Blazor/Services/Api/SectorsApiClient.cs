using System.Net.Http.Json;
using SafetyScale.Web.Blazor.Models.Sectors;

namespace SafetyScale.Web.Blazor.Services.Api;

/// <summary>
/// Sectors API client. Parity with React <c>sectorsApi.ts</c>.
/// </summary>
public sealed class SectorsApiClient(ApiHttpClient apiClient)
{
    /// <summary>
    /// Lists sectors. <paramref name="isActive"/> <c>null</c> returns all sectors.
    /// </summary>
    public async Task<IReadOnlyList<SectorDto>> ListAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var path = isActive.HasValue
            ? $"/api/sectors?isActive={isActive.Value.ToString().ToLowerInvariant()}"
            : "/api/sectors";

        using var response = await apiClient.GetAsync(path, cancellationToken: cancellationToken);
        await ApiClientResponseHelper.EnsureOkAsync(
            response,
            "Não foi possível carregar setores.",
            cancellationToken);

        var list = await apiClient.ReadJsonAsync<List<SectorDto>>(response, cancellationToken);
        return list ?? [];
    }

    public async Task<Guid> CreateAsync(
        CreateSectorRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var body = NormalizeCreateRequest(request);

        using var response = await apiClient.PostJsonAsync(
            "/api/sectors",
            body,
            cancellationToken: cancellationToken);

        await ApiClientResponseHelper.EnsureOkAsync(
            response,
            "Não foi possível criar setor.",
            cancellationToken);

        var created = await apiClient.ReadJsonAsync<CreateSectorResponseDto>(response, cancellationToken);
        if (created is null || created.Id == Guid.Empty)
        {
            throw new ApiException((int)response.StatusCode, null, "Não foi possível criar setor.");
        }

        return created.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        UpdateSectorRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var body = NormalizeUpdateRequest(request);
        var path = $"/api/sectors/{Uri.EscapeDataString(id.ToString())}";

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
        var path = $"/api/sectors/{Uri.EscapeDataString(id.ToString())}/inactive";

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
        var path = $"/api/sectors/{Uri.EscapeDataString(id.ToString())}/active";

        using var response = await apiClient.SendAsync(
            path,
            HttpMethod.Patch,
            cancellationToken: cancellationToken);

        await ApiClientResponseHelper.EnsureOkAsync(
            response,
            "Não foi possível reativar.",
            cancellationToken);
    }

    private static CreateSectorRequestDto NormalizeCreateRequest(CreateSectorRequestDto request) =>
        request with { Description = NormalizeDescription(request.Description) };

    private static UpdateSectorRequestDto NormalizeUpdateRequest(UpdateSectorRequestDto request) =>
        request with { Description = NormalizeDescription(request.Description) };

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
