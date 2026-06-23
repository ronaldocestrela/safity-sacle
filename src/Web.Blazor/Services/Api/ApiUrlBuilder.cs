namespace SafetyScale.Web.Blazor.Services.Api;

public sealed class ApiUrlBuilder(AppConfiguration configuration)
{
    public string Build(string path)
    {
        var normalizedPath = path.StartsWith('/') ? path : $"/{path}";
        return string.IsNullOrEmpty(configuration.ApiBaseUrl)
            ? normalizedPath
            : $"{configuration.ApiBaseUrl}{normalizedPath}";
    }
}
