namespace SafetyScale.Web.Blazor.Models.Auth;

public sealed record PlatformAuthSession(
    string Token,
    string? Email,
    IReadOnlyList<PlatformUserRole> Roles);
