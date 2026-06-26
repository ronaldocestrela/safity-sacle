using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SafetyScale.Application.Abstractions.Authentication;

namespace SafetyScale.Infrastructure.Authentication;

public sealed class HttpContextCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public string? UserId =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public IReadOnlyList<string> Roles =>
        httpContextAccessor.HttpContext?.User
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList()
        ?? [];

    public Guid? SecurityGuardId
    {
        get
        {
            var raw = httpContextAccessor.HttpContext?.User.FindFirstValue(AuthClaimTypes.SecurityGuardId);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public bool IsInRole(string role) =>
        Roles.Contains(role, StringComparer.Ordinal);
}
