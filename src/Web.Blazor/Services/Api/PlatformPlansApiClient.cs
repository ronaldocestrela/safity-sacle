using SafetyScale.Web.Blazor.Models.Platform;

namespace SafetyScale.Web.Blazor.Services.Api;

public sealed class PlatformPlansApiClient(ApiHttpClient apiClient)
{
    public async Task<IReadOnlyList<PlatformPlanDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var response = await apiClient.GetAsync("/api/platform/plans", cancellationToken: cancellationToken);
        response.EnsureSuccessStatusCode();
        return await apiClient.ReadJsonAsync<IReadOnlyList<PlatformPlanDto>>(response, cancellationToken)
               ?? Array.Empty<PlatformPlanDto>();
    }

    public async Task<IReadOnlyList<PlatformPlanDto>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        using var response = await apiClient.GetAsync("/api/platform/plans/active", cancellationToken: cancellationToken);
        response.EnsureSuccessStatusCode();
        return await apiClient.ReadJsonAsync<IReadOnlyList<PlatformPlanDto>>(response, cancellationToken)
               ?? Array.Empty<PlatformPlanDto>();
    }

    public async Task<CreatePlatformPlanOutcome> CreateAsync(
        CreatePlatformPlanRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await apiClient.PostJsonAsync(
                "/api/platform/plans",
                request,
                cancellationToken: cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                return CreatePlatformPlanOutcome.Success();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var message = await ApiErrorReader.ReadMessageAsync(response, cancellationToken);
                return CreatePlatformPlanOutcome.Fail(message ?? "Conflito ao criar plano.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var message = await ApiErrorReader.ReadMessageAsync(response, cancellationToken);
                return CreatePlatformPlanOutcome.Fail(message ?? "Dados inválidos.");
            }

            return CreatePlatformPlanOutcome.Fail("Não foi possível conectar à API.");
        }
        catch (HttpRequestException)
        {
            return CreatePlatformPlanOutcome.Fail("Não foi possível conectar à API.");
        }
    }

    public async Task<UpdatePlatformPlanOutcome> UpdateAsync(
        Guid planId,
        UpdatePlatformPlanRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await apiClient.PutJsonAsync(
                $"/api/platform/plans/{planId}",
                request,
                cancellationToken: cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return UpdatePlatformPlanOutcome.Success();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return UpdatePlatformPlanOutcome.Fail("Plano não encontrado.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var message = await ApiErrorReader.ReadMessageAsync(response, cancellationToken);
                return UpdatePlatformPlanOutcome.Fail(message ?? "Dados inválidos.");
            }

            return UpdatePlatformPlanOutcome.Fail("Não foi possível conectar à API.");
        }
        catch (HttpRequestException)
        {
            return UpdatePlatformPlanOutcome.Fail("Não foi possível conectar à API.");
        }
    }

    public async Task<bool> ActivateAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        using var response = await apiClient.PatchAsync(
            $"/api/platform/plans/{planId}/activate",
            content: null,
            cancellationToken: cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeactivateAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        using var response = await apiClient.PatchAsync(
            $"/api/platform/plans/{planId}/deactivate",
            content: null,
            cancellationToken: cancellationToken);
        return response.IsSuccessStatusCode;
    }
}

public sealed record CreatePlatformPlanOutcome(bool Ok, string? Message = null)
{
    public static CreatePlatformPlanOutcome Success() => new(true);

    public static CreatePlatformPlanOutcome Fail(string message) => new(false, message);
}

public sealed record UpdatePlatformPlanOutcome(bool Ok, string? Message = null)
{
    public static UpdatePlatformPlanOutcome Success() => new(true);

    public static UpdatePlatformPlanOutcome Fail(string message) => new(false, message);
}
