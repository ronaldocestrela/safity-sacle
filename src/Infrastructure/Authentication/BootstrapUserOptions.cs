namespace SafetyScale.Infrastructure.Authentication;

public sealed class BootstrapUserOptions
{
    public const string SectionName = "BootstrapUser";

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Role { get; set; } = "PlatformOwner";
}
