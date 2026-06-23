using System.Net;
using System.Net.Http.Json;
using SafetyScale.Web.Blazor.Models.UnavailableDays;

namespace SafetyScale.Web.Blazor.Services.Api;

/// <summary>
/// Unavailable days API client. Parity with React <c>unavailableDaysApi.ts</c>.
/// </summary>
public sealed class UnavailableDaysApiClient(ApiHttpClient apiClient)
{
    public async Task<IReadOnlyList<UnavailableDayDto>> ListByGuardAsync(
        Guid guardId,
        CancellationToken cancellationToken = default)
    {
        var path = $"/api/security-guards/{Uri.EscapeDataString(guardId.ToString())}/unavailable-days";

        using var response = await apiClient.GetAsync(path, cancellationToken: cancellationToken);
        await EnsureOkAsync(response, "Could not load restrictions.", cancellationToken);

        var list = await apiClient.ReadJsonAsync<List<UnavailableDayDto>>(response, cancellationToken);
        return list ?? [];
    }

    public async Task<Guid> AddAsync(
        Guid guardId,
        AddUnavailableDayRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var path = $"/api/security-guards/{Uri.EscapeDataString(guardId.ToString())}/unavailable-days";
        var body = request with
        {
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
        };

        using var response = await apiClient.PostJsonAsync(path, body, cancellationToken: cancellationToken);
        await EnsureOkAsync(response, "Could not save restriction.", cancellationToken);

        var created = await apiClient.ReadJsonAsync<CreateUnavailableDayResponseDto>(response, cancellationToken);
        if (created is null || created.Id == Guid.Empty)
        {
            throw new ApiException((int)response.StatusCode, null, "Could not save restriction.");
        }

        return created.Id;
    }

    public async Task DeleteAsync(Guid unavailableDayId, CancellationToken cancellationToken = default)
    {
        var path = $"/api/unavailable-days/{Uri.EscapeDataString(unavailableDayId.ToString())}";

        using var response = await apiClient.SendAsync(
            path,
            HttpMethod.Delete,
            cancellationToken: cancellationToken);

        await EnsureOkAsync(response, "Could not remove restriction.", cancellationToken);
    }

    private static async Task EnsureOkAsync(
        HttpResponseMessage response,
        string fallback,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = await ApiErrorReader.ReadMessageAsync(response, cancellationToken);
        message = response.StatusCode switch
        {
            HttpStatusCode.Forbidden => message ?? "Você não tem permissão para esta ação.",
            HttpStatusCode.NotFound => message ?? "Registro não encontrado.",
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity =>
                message ?? "Dados inválidos. Confira os campos.",
            HttpStatusCode.Conflict =>
                message ?? "This date is already marked unavailable for this personnel.",
            _ => message,
        };

        throw new ApiException((int)response.StatusCode, message, fallback);
    }
}
