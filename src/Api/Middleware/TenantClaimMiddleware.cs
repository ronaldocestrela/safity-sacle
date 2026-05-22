using SafetyScale.Application.Abstractions.Tenancy;
using SafetyScale.Infrastructure.Authentication;

namespace SafetyScale.Api.Middleware;

public sealed class TenantClaimMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantExecutionContext tenantExecution)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
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
