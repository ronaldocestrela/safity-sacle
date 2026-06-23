using System.Net;

namespace SafetyScale.Web.Blazor.Services.Api;

internal static class ApiClientResponseHelper
{
    public static async Task EnsureOkAsync(
        HttpResponseMessage response,
        string fallback,
        CancellationToken cancellationToken = default)
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
            HttpStatusCode.Conflict => message ?? "Conflito ao processar a solicitação.",
            _ => message,
        };

        throw new ApiException((int)response.StatusCode, message, fallback);
    }
}
