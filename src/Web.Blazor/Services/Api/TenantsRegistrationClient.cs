using System.Net;
using SafetyScale.Web.Blazor.Models.Tenants;

namespace SafetyScale.Web.Blazor.Services.Api;

/// <summary>
/// Tenant registration API client. Parity with React <c>registerTenantApi.ts</c>.
/// </summary>
public sealed class TenantsRegistrationClient(ApiHttpClient apiClient)
{
    private static readonly ApiRequestOptions PublicRequest = new() { SkipAuthRedirect = true };

    private const string DefaultInvalidPasswordMessage =
        "A senha deve ter pelo menos 8 caracteres, incluindo maiúsculas, minúsculas, números e um carácter especial.";

    private const string NetworkErrorMessage =
        "Não foi possível conectar à API. Verifique se o servidor está no ar.";

    public async Task<RegisterTenantOutcome> RegisterAsync(
        RegisterTenantRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await apiClient.PostJsonAsync(
                "/api/tenants/register",
                request,
                PublicRequest,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                var body = await apiClient.ReadJsonAsync<RegisterTenantResponseDto>(response, cancellationToken);
                if (body is null ||
                    body.TenantId == Guid.Empty ||
                    string.IsNullOrWhiteSpace(body.AdminUserId) ||
                    string.IsNullOrWhiteSpace(body.TenantSlug))
                {
                    return RegisterTenantOutcome.Fail(RegisterTenantFailureReason.Network);
                }

                return RegisterTenantOutcome.Success(body);
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                var message = await ApiErrorReader.ReadMessageAsync(response, cancellationToken) ?? string.Empty;
                if (message.Contains("identificador", StringComparison.OrdinalIgnoreCase))
                {
                    return RegisterTenantOutcome.Fail(
                        RegisterTenantFailureReason.TenantExists,
                        "Não foi possível concluir o cadastro. Tente alterar o nome da empresa.");
                }

                return RegisterTenantOutcome.Fail(
                    RegisterTenantFailureReason.EmailExists,
                    "Este e-mail já está cadastrado.");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var message = await ApiErrorReader.ReadMessageAsync(response, cancellationToken);
                var messages = string.IsNullOrWhiteSpace(message) ? null : new[] { message };

                if (IsPasswordRelated(message))
                {
                    return RegisterTenantOutcome.Fail(
                        RegisterTenantFailureReason.InvalidPassword,
                        messages is { Length: > 0 } ? message : DefaultInvalidPasswordMessage,
                        messages);
                }

                return RegisterTenantOutcome.Fail(
                    RegisterTenantFailureReason.Validation,
                    message,
                    messages);
            }

            return RegisterTenantOutcome.Fail(RegisterTenantFailureReason.Network, NetworkErrorMessage);
        }
        catch (HttpRequestException)
        {
            return RegisterTenantOutcome.Fail(RegisterTenantFailureReason.Network, NetworkErrorMessage);
        }
    }

    private static bool IsPasswordRelated(string? message) =>
        !string.IsNullOrWhiteSpace(message) &&
        (message.Contains("senha", StringComparison.OrdinalIgnoreCase) ||
         message.Contains("password", StringComparison.OrdinalIgnoreCase));
}

public enum RegisterTenantFailureReason
{
    TenantExists,
    EmailExists,
    InvalidPassword,
    Validation,
    Network,
}

public sealed record RegisterTenantOutcome(
    bool Ok,
    RegisterTenantResponseDto? Response = null,
    RegisterTenantFailureReason? Reason = null,
    string? Message = null,
    IReadOnlyList<string>? Messages = null)
{
    public static RegisterTenantOutcome Success(RegisterTenantResponseDto response) =>
        new(true, response);

    public static RegisterTenantOutcome Fail(
        RegisterTenantFailureReason reason,
        string? message = null,
        IReadOnlyList<string>? messages = null) =>
        new(false, Reason: reason, Message: message, Messages: messages);
}
