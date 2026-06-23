using System.Text.Json;
using Microsoft.JSInterop;

namespace SafetyScale.Web.Blazor.Services.Auth;

public sealed class BrowserSessionStorage(IJSRuntime jsRuntime, JsonSerializerOptions jsonOptions)
{
    public const string AuthSessionStorageKey = "safetyscale.auth.session";

    private sealed record StoredShape(string Token);

    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var raw = await jsRuntime.InvokeAsync<string?>(
            "sessionStorageInterop.getItem",
            cancellationToken,
            AuthSessionStorageKey);

        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<StoredShape>(raw, jsonOptions);
            return parsed?.Token;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task SaveTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(new StoredShape(token), jsonOptions);
        await jsRuntime.InvokeVoidAsync(
            "sessionStorageInterop.setItem",
            cancellationToken,
            AuthSessionStorageKey,
            json);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        jsRuntime.InvokeVoidAsync(
            "sessionStorageInterop.removeItem",
            cancellationToken,
            AuthSessionStorageKey).AsTask();
}
