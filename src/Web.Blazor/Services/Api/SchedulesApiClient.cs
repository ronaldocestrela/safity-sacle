using System.Net;
using System.Text.Json;
using SafetyScale.Web.Blazor.Models.Schedules;

namespace SafetyScale.Web.Blazor.Services.Api;

/// <summary>
/// Schedules API client. Parity with React <c>schedulesApi.ts</c>.
/// </summary>
public sealed class SchedulesApiClient(ApiHttpClient apiClient)
{
    public async Task<MonthlyScheduleDto?> GetByMonthYearAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        using var response = await apiClient.GetAsync(
            $"/api/schedules/month/{month}/year/{year}",
            cancellationToken: cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        var period = MonthYearMessage(month, year);
        await EnsureOkAsync(response, $"Could not load schedule for {period}.", cancellationToken);

        return await apiClient.ReadJsonAsync<MonthlyScheduleDto>(response, cancellationToken);
    }

    public async Task<Guid> GenerateAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        var period = MonthYearMessage(month, year);
        var body = new GenerateMonthlyScheduleRequestDto(month, year);

        using var response = await apiClient.PostJsonAsync(
            "/api/schedules/generate",
            body,
            cancellationToken: cancellationToken);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var created = await apiClient.ReadJsonAsync<CreateScheduleResponseDto>(response, cancellationToken);
            if (created is null || created.Id == Guid.Empty)
            {
                throw new ApiException((int)response.StatusCode, null, "Resposta inesperada ao gerar a escala.");
            }

            return created.Id;
        }

        await EnsureOkAsync(
            response,
            $"Não foi possível gerar a escala para {period}.",
            cancellationToken);

        throw new ApiException((int)response.StatusCode, null, "Resposta inesperada ao gerar a escala.");
    }

    private async Task EnsureOkAsync(
        HttpResponseMessage response,
        string fallback,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = ApiErrorReader.ParseJsonBody(body);

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
        {
            var coverage = TryParseCoverageFailure(body);
            if (!string.IsNullOrWhiteSpace(coverage?.Message))
            {
                message = coverage.Message;
            }
        }

        message = response.StatusCode switch
        {
            HttpStatusCode.Forbidden => message ?? "Você não tem permissão para esta ação.",
            HttpStatusCode.NotFound => message ?? "Registro não encontrado.",
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity =>
                message ?? "Dados inválidos. Confira os campos.",
            HttpStatusCode.Conflict => message ?? "Escala já gerada para este mês e ano.",
            _ => message,
        };

        throw new ApiException((int)response.StatusCode, message, fallback);
    }

    private ScheduleCoverageFailureResponse? TryParseCoverageFailure(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ScheduleCoverageFailureResponse>(body, apiClient.JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string MonthYearMessage(int month, int year) =>
        $"{month:D2}/{year}";
}
