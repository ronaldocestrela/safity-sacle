using System.Net.Http.Json;
using SafetyScale.Web.Blazor.Models.Billing;

namespace SafetyScale.Web.Blazor.Services.Api;

public sealed class BillingApiClient(ApiHttpClient apiClient)
{
    public async Task<IReadOnlyList<BillingPlanDto>> ListPlansAsync(CancellationToken cancellationToken = default)
    {
        using var response = await apiClient.GetAsync("/api/billing/plans", cancellationToken: cancellationToken);
        await ApiClientResponseHelper.EnsureOkAsync(
            response,
            "Não foi possível carregar os planos.",
            cancellationToken);

        return await apiClient.ReadJsonAsync<IReadOnlyList<BillingPlanDto>>(response, cancellationToken)
               ?? Array.Empty<BillingPlanDto>();
    }

    public async Task<TenantBillingStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        using var response = await apiClient.GetAsync("/api/billing/status", cancellationToken: cancellationToken);
        await ApiClientResponseHelper.EnsureOkAsync(
            response,
            "Não foi possível carregar o status da assinatura.",
            cancellationToken);

        return await apiClient.ReadJsonAsync<TenantBillingStatusDto>(response, cancellationToken)
               ?? throw new ApiException((int)response.StatusCode, null, "Resposta inválida da API.");
    }

    public async Task<string> CreateCheckoutSessionAsync(
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        using var response = await apiClient.PostJsonAsync(
            "/api/billing/checkout-session",
            new CreateCheckoutSessionRequestDto(planId),
            cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = await ApiErrorReader.ReadMessageAsync(response, cancellationToken);
            throw new ApiException((int)response.StatusCode, null, message ?? "Não foi possível iniciar o checkout.");
        }

        var payload = await apiClient.ReadJsonAsync<CheckoutSessionResponseDto>(response, cancellationToken);
        if (payload is null || string.IsNullOrWhiteSpace(payload.CheckoutUrl))
        {
            throw new ApiException((int)response.StatusCode, null, "Checkout inválido retornado pela API.");
        }

        return payload.CheckoutUrl;
    }

    public async Task<string> CreatePortalSessionAsync(CancellationToken cancellationToken = default)
    {
        using var response = await apiClient.PostJsonAsync(
            "/api/billing/portal-session",
            new { },
            cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = await ApiErrorReader.ReadMessageAsync(response, cancellationToken);
            throw new ApiException((int)response.StatusCode, null, message ?? "Não foi possível abrir o portal.");
        }

        var payload = await apiClient.ReadJsonAsync<PortalSessionResponseDto>(response, cancellationToken);
        if (payload is null || string.IsNullOrWhiteSpace(payload.PortalUrl))
        {
            throw new ApiException((int)response.StatusCode, null, "Portal inválido retornado pela API.");
        }

        return payload.PortalUrl;
    }
}
