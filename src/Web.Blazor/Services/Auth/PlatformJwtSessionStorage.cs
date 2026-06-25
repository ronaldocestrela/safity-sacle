using SafetyScale.Web.Blazor.Models.Auth;

namespace SafetyScale.Web.Blazor.Services.Auth;

public sealed class PlatformJwtSessionStorage(PlatformBrowserSessionStorage browserSessionStorage)
{
    public async Task<string?> GetStoredTokenAsync(CancellationToken cancellationToken = default) =>
        await browserSessionStorage.GetTokenAsync(cancellationToken);

    public async Task<PlatformAuthSession?> GetSessionAsync(CancellationToken cancellationToken = default)
    {
        var token = await GetStoredTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var session = JwtParser.BuildPlatformSessionFromToken(token);
        if (session is null)
        {
            await ClearAsync(cancellationToken);
            return null;
        }

        return session;
    }

    public async Task<PlatformAuthSession?> SaveTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var session = JwtParser.BuildPlatformSessionFromToken(token);
        if (session is null)
        {
            return null;
        }

        await browserSessionStorage.SaveTokenAsync(token, cancellationToken);
        return session;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        browserSessionStorage.ClearAsync(cancellationToken);
}
