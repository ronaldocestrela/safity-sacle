namespace SafetyScale.Web.Blazor.Services.Api;

public sealed class AppConfiguration
{
    public AppConfiguration(IConfiguration configuration)
    {
        ApiBaseUrl = NormalizeApiBase(configuration["ApiBaseUrl"]);
    }

    public string ApiBaseUrl { get; }

    internal static string NormalizeApiBase(string? raw)
    {
        var t = raw?.Trim();
        if (string.IsNullOrEmpty(t))
        {
            return string.Empty;
        }

        return t.TrimEnd('/');
    }
}
