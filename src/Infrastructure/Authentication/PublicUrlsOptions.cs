namespace SafetyScale.Infrastructure.Authentication;

public sealed class PublicUrlsOptions
{
    public const string SectionName = "PublicUrls";

    public string WebBaseUrl { get; set; } = "http://localhost:4864";
}
