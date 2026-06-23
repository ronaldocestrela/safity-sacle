using SafetyScale.Web.Blazor.Models.SecurityGuards;

namespace SafetyScale.Web.Blazor.Services.Api;

/// <summary>
/// Security guards API client (read-only subset for B5.1). Full CRUD in B7.1.
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
}
