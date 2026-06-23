using SafetyScale.Web.Blazor.Models.Auth;

namespace SafetyScale.Web.Blazor.Services.Auth;

/// <summary>
/// Session persistence backed by browser sessionStorage. Parity with React <c>session.ts</c>.
/// </summary>
public sealed class JwtSessionStorage(BrowserSessionStorage browserSessionStorage)
{
    public async Task<string?> GetStoredTokenAsync(CancellationToken cancellationToken = default) =>
        await browserSessionStorage.GetTokenAsync(cancellationToken);

    public async Task<AuthSession?> GetSessionAsync(CancellationToken cancellationToken = default)
    {
        var token = await GetStoredTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var session = JwtParser.BuildSessionFromToken(token);
        if (session is null)
        {
            await ClearAsync(cancellationToken);
            return null;
        }

        return session;
    }

    public async Task<AuthSession?> SaveTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var session = JwtParser.BuildSessionFromToken(token);
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
