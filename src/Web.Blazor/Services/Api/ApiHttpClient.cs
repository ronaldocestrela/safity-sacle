using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SafetyScale.Web.Blazor.Services.Api;

/// <summary>
/// Shared HTTP client for API calls. Uses <see cref="ApiUrlBuilder"/> for every URL.
/// </summary>
public sealed class ApiHttpClient(HttpClient httpClient, ApiUrlBuilder urlBuilder, JsonSerializerOptions jsonOptions)
{
    public JsonSerializerOptions JsonOptions => jsonOptions;

    public Task<HttpResponseMessage> SendAsync(
        string path,
        HttpMethod method,
        HttpContent? content = null,
        Action<HttpRequestMessage>? configure = null,
        ApiRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var url = urlBuilder.Build(path);
        var request = new HttpRequestMessage(method, url) { Content = content };

        ApplyOptions(request, options);
        configure?.Invoke(request);

        return httpClient.SendAsync(request, cancellationToken);
    }

    public Task<HttpResponseMessage> GetAsync(
        string path,
        Action<HttpRequestMessage>? configure = null,
        ApiRequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        SendAsync(path, HttpMethod.Get, configure: configure, options: options, cancellationToken: cancellationToken);

    public Task<HttpResponseMessage> PostJsonAsync<TBody>(
        string path,
        TBody body,
        ApiRequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            path,
            HttpMethod.Post,
            JsonContent.Create(body, options: jsonOptions),
            options: options,
            cancellationToken: cancellationToken);

    public Task<HttpResponseMessage> PutJsonAsync<TBody>(
        string path,
        TBody body,
        ApiRequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            path,
            HttpMethod.Put,
            JsonContent.Create(body, options: jsonOptions),
            options: options,
            cancellationToken: cancellationToken);

    public Task<HttpResponseMessage> PatchAsync(
        string path,
        HttpContent? content = null,
        ApiRequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        SendAsync(path, HttpMethod.Patch, content, options: options, cancellationToken: cancellationToken);

    public Task<T?> ReadJsonAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default) =>
        response.Content.ReadFromJsonAsync<T>(jsonOptions, cancellationToken);

    public static void SetBearer(HttpRequestMessage request, string token) =>
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private static void ApplyOptions(HttpRequestMessage request, ApiRequestOptions? options)
    {
        var opts = options ?? ApiRequestOptions.Default;
        request.Options.Set(ApiHttpContext.SkipAuthRedirectKey, opts.SkipAuthRedirect);
        request.Options.Set(ApiHttpContext.SkipBearerInjectionKey, opts.SkipBearerInjection);
    }
}
