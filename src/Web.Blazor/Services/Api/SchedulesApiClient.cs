using System.Net;
using SafetyScale.Web.Blazor.Models.Schedules;

namespace SafetyScale.Web.Blazor.Services.Api;

/// <summary>
/// Schedules API client (read-only subset for B5.1). Full module in B9.1.
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

        var period = $"{month:D2}/{year}";
        await ApiClientResponseHelper.EnsureOkAsync(
            response,
            $"Could not load schedule for {period}.",
            cancellationToken);

        return await apiClient.ReadJsonAsync<MonthlyScheduleDto>(response, cancellationToken);
    }
}
