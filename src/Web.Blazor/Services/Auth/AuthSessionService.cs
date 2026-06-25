using System.Text.Json;
using Microsoft.AspNetCore.Components;
using SafetyScale.Web.Blazor.Models.Auth;
using SafetyScale.Web.Blazor.Services.Api;

namespace SafetyScale.Web.Blazor.Services.Auth;

/// <summary>
/// High-level auth operations: login, logout, session access. Parity with React <c>AuthProvider</c>.
/// </summary>
public sealed class AuthSessionService(
    ApiHttpClient apiClient,
    JwtSessionStorage sessionStorage,
    CustomAuthStateProvider authStateProvider,
    NavigationManager navigationManager)
{
    private static readonly ApiRequestOptions PublicRequest = new() { SkipAuthRedirect = true };

    public Task<AuthSession?> GetSessionAsync(CancellationToken cancellationToken = default) =>
        sessionStorage.GetSessionAsync(cancellationToken);

    public async Task<LoginAttemptResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await apiClient.PostJsonAsync(
                "/api/auth/login",
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

    public async Task<ConfirmEmailOutcome> ConfirmEmailAsync(
        string userId,
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await apiClient.PostJsonAsync(
                "/api/auth/confirm-email",
                new ConfirmEmailRequestDto(userId, token),
                PublicRequest,
                cancellationToken);

            var message = await ApiErrorReader.ReadMessageAsync(response, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return message?.Contains("já estava", StringComparison.OrdinalIgnoreCase) == true
                    ? new ConfirmEmailOutcome(ConfirmEmailOutcomeStatus.AlreadyConfirmed, message)
                    : new ConfirmEmailOutcome(ConfirmEmailOutcomeStatus.Success, message);
            }

            return response.StatusCode switch
            {
                System.Net.HttpStatusCode.BadRequest =>
                    new ConfirmEmailOutcome(ConfirmEmailOutcomeStatus.InvalidToken, message),
                System.Net.HttpStatusCode.NotFound =>
                    new ConfirmEmailOutcome(ConfirmEmailOutcomeStatus.UserNotFound, message),
                _ => new ConfirmEmailOutcome(ConfirmEmailOutcomeStatus.Network, message),
            };
        }
        catch (HttpRequestException)
        {
            return new ConfirmEmailOutcome(ConfirmEmailOutcomeStatus.Network);
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
            navigationManager.NavigateTo("/login", replace: true);
        }
    }

    /// <summary>
    /// Clears session and notifies auth state without navigation (401 handler navigates separately).
    /// </summary>
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
