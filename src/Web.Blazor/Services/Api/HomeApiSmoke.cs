namespace SafetyScale.Web.Blazor.Services.Api;

/// <summary>
/// Optional API smoke for the public home page. Parity with React <c>apiSmoke.ts</c> (simplified — no dev login retry).
/// </summary>
public static class HomeApiSmoke
{
    private static readonly ApiRequestOptions PublicRequest = new()
    {
        SkipAuthRedirect = true,
        SkipBearerInjection = true,
    };

    public static async Task<HomeSmokeResult> RunAsync(ApiHttpClient apiClient, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await apiClient.GetAsync("/api/health", options: PublicRequest, cancellationToken: cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return HomeSmokeResult.Ok($"GET /api/health OK: {body}");
            }

            var statusCode = (int)response.StatusCode;
            if (statusCode is 401 or 403)
            {
                return HomeSmokeResult.Ok(
                    "API respondeu (sem token em /api/health). Faça login para acessar recursos autenticados.");
            }

            return HomeSmokeResult.Error($"GET /api/health inesperado: HTTP {statusCode}");
        }
        catch (HttpRequestException ex)
        {
            return HomeSmokeResult.Error(
                $"Falha de rede ou CORS: {ex.Message}. Suba a API (dotnet run --project src/Api) e confirme CORS para http://localhost:4864.");
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return HomeSmokeResult.Error($"Falha ao verificar API: {ex.Message}");
        }
    }
}

public enum HomeSmokeState
{
    Loading,
    Ok,
    Error,
}

public sealed record HomeSmokeResult(HomeSmokeState State, string Message)
{
    public static HomeSmokeResult Ok(string message) => new(HomeSmokeState.Ok, message);

    public static HomeSmokeResult Error(string message) => new(HomeSmokeState.Error, message);
}
