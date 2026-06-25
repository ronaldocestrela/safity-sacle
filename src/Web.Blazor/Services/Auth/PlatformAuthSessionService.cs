using System.Text.Json;
using Microsoft.AspNetCore.Components;
using SafetyScale.Web.Blazor.Models.Auth;
using SafetyScale.Web.Blazor.Services.Api;

namespace SafetyScale.Web.Blazor.Services.Auth;

public sealed class PlatformAuthSessionService(
    ApiHttpClient apiClient,
    PlatformJwtSessionStorage sessionStorage,
    CustomAuthStateProvider authStateProvider,
    NavigationManager navigationManager)
{
    private static readonly ApiRequestOptions PublicRequest = new() { SkipAuthRedirect = true };

    public Task<PlatformAuthSession?> GetSessionAsync(CancellationToken cancellationToken = default) =>
        sessionStorage.GetSessionAsync(cancellationToken);

    public async Task<LoginAttemptResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await apiClient.PostJsonAsync(
                "/api/auth/platform/login",
                new LoginRequestDto(email.Trim(), password),
                PublicRequest,
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (IsEmailNotConfirmed(body))
                {
                    return new LoginAttemptResult(false, LoginFailureReason.EmailNotConfirmed);
                }

                return new LoginAttemptResult(false, LoginFailureReason.Invalid);
            }

            if (!response.IsSuccessStatusCode)
            {
                return new LoginAttemptResult(false, LoginFailureReason.Network);
            }

            var loginResponse = await apiClient.ReadJsonAsync<LoginResponseDto>(response, cancellationToken);
            if (loginResponse is null || string.IsNullOrEmpty(loginResponse.Token))
            {
                return new LoginAttemptResult(false, LoginFailureReason.Network);
            }

            var session = await sessionStorage.SaveTokenAsync(loginResponse.Token, cancellationToken);
            if (session is null)
            {
                return new LoginAttemptResult(false, LoginFailureReason.Network);
            }

            authStateProvider.NotifyAuthenticationStateChanged();
            return new LoginAttemptResult(true);
        }
        catch (HttpRequestException)
        {
            return new LoginAttemptResult(false, LoginFailureReason.Network);
        }
    }

    public async Task LogoutAsync(
        bool navigateToLogin = false,
        CancellationToken cancellationToken = default)
    {
        await sessionStorage.ClearAsync(cancellationToken);
        authStateProvider.NotifyAuthenticationStateChanged();

        if (navigateToLogin)
        {
            navigationManager.NavigateTo("/platform/login", replace: true);
        }
    }

    public async Task InvalidateSessionAsync(CancellationToken cancellationToken = default)
    {
        await sessionStorage.ClearAsync(cancellationToken);
        authStateProvider.NotifyAuthenticationStateChanged();
    }

    private static bool IsEmailNotConfirmed(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("code", out var codeElement) &&
                codeElement.ValueKind == JsonValueKind.String)
            {
                return string.Equals(
                    codeElement.GetString(),
                    "email_not_confirmed",
                    StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (JsonException)
        {
        }

        return false;
    }
}
