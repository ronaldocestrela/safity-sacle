namespace SafetyScale.Web.Blazor.Models.Auth;

/// <summary>
/// Authenticated session derived from JWT. Parity with React <c>AuthSession</c>.
/// </summary>
public sealed record AuthSession(
    string Token,
    string? Email,
    IReadOnlyList<UserRole> Roles,
    string TenantId);
