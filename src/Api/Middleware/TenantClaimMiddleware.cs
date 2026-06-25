using SafetyScale.Application.Abstractions.Tenancy;
using SafetyScale.Infrastructure.Authentication;
using SafetyScale.Infrastructure.Identity;

namespace SafetyScale.Api.Middleware;

public sealed class TenantClaimMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantExecutionContext tenantExecution)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userKind = context.User.FindFirst(AuthClaimTypes.UserKind)?.Value;
            if (string.Equals(userKind, UserKind.Platform.ToString(), StringComparison.Ordinal))
            {
                await next(context);
                return;
            }

            var raw = context.User.FindFirst(TenantClaimTypes.TenantId)?.Value;
            if (string.IsNullOrWhiteSpace(raw) || !Guid.TryParse(raw, out var tenantId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            tenantExecution.SetExecutingTenant(tenantId);
        }

        await next(context);
    }
}
