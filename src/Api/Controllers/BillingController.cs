using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafetyScale.Api.Contracts.Billing;
using SafetyScale.Application.Abstractions.Billing;
using SafetyScale.Application.Abstractions.Tenancy;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Api.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize(Roles = "Admin")]
public sealed class BillingController(
    IBillingService billingService,
    ITenantExecutionContext tenantExecution) : ControllerBase
{
    [HttpGet("plans")]
    [ProducesResponseType(typeof(IReadOnlyList<BillingPlanResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPlans(CancellationToken cancellationToken)
    {
        var plans = await billingService.ListAvailablePlansAsync(cancellationToken);
        return Ok(plans.Select(MapPlan).ToList());
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(TenantBillingStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        if (tenantExecution.TenantId is not Guid tenantId)
        {
            return NotFound(new { message = "Tenant não encontrado." });
        }

        var status = await billingService.GetTenantBillingStatusAsync(tenantId, cancellationToken);
        if (status is null)
        {
            return NotFound(new { message = "Tenant não encontrado." });
        }

        return Ok(MapStatus(status));
    }

    [HttpPost("checkout-session")]
    [ProducesResponseType(typeof(CheckoutSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateCheckoutSession(
        [FromBody] CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (tenantExecution.TenantId is not Guid tenantId)
        {
            return NotFound(new { message = "Tenant não encontrado." });
        }

        var adminEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? User.Identity?.Name
            ?? string.Empty;

        var result = await billingService.CreateCheckoutSessionAsync(
            tenantId,
            adminEmail,
            new CreateCheckoutSessionInput(request.PlanId),
            cancellationToken);

        return result.Status switch
        {
            CreateCheckoutSessionStatus.Success =>
                Ok(new CheckoutSessionResponse(result.CheckoutUrl!)),

            CreateCheckoutSessionStatus.TenantNotFound =>
                NotFound(new { message = "Tenant não encontrado." }),

            CreateCheckoutSessionStatus.PlanNotFound =>
                NotFound(new { message = "Plano não encontrado." }),

            CreateCheckoutSessionStatus.PlanInactive or CreateCheckoutSessionStatus.PlanNotConfigured =>
                BadRequest(new { message = "Plano indisponível para assinatura." }),

            CreateCheckoutSessionStatus.StripeNotConfigured =>
                StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Pagamentos não configurados." }),

            CreateCheckoutSessionStatus.StripeError =>
                BadRequest(new { message = result.ErrorMessage ?? "Erro ao iniciar checkout." }),

            _ => throw new InvalidOperationException($"Unexpected checkout status {result.Status}."),
        };
    }

    [HttpPost("portal-session")]
    [ProducesResponseType(typeof(PortalSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreatePortalSession(CancellationToken cancellationToken)
    {
        if (tenantExecution.TenantId is not Guid tenantId)
        {
            return NotFound(new { message = "Tenant não encontrado." });
        }

        var result = await billingService.CreatePortalSessionAsync(tenantId, cancellationToken);

        return result.Status switch
        {
            CreatePortalSessionStatus.Success =>
                Ok(new PortalSessionResponse(result.PortalUrl!)),

            CreatePortalSessionStatus.TenantNotFound =>
                NotFound(new { message = "Tenant não encontrado." }),

            CreatePortalSessionStatus.NoStripeCustomer =>
                BadRequest(new { message = "Nenhuma assinatura Stripe vinculada a este tenant." }),

            CreatePortalSessionStatus.StripeNotConfigured =>
                StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Pagamentos não configurados." }),

            CreatePortalSessionStatus.StripeError =>
                BadRequest(new { message = result.ErrorMessage ?? "Erro ao abrir portal." }),

            _ => throw new InvalidOperationException($"Unexpected portal status {result.Status}."),
        };
    }

    private static BillingPlanResponse MapPlan(BillingPlanDto plan) =>
        new(
            plan.Id,
            plan.Name,
            plan.Code,
            plan.Description,
            plan.PriceMonthly,
            plan.MaxSecurityGuards,
            plan.MaxSectors,
            plan.HasStripePrice);

    private static TenantBillingStatusResponse MapStatus(TenantBillingStatusDto status) =>
        new(
            status.TenantId,
            status.BillingStatus.ToString(),
            status.LeadStatus.ToString(),
            status.PlatformPlanId,
            status.PlatformPlanName,
            status.CurrentPeriodEnd,
            status.HasActiveSubscription,
            status.CanManageSubscription);
}
