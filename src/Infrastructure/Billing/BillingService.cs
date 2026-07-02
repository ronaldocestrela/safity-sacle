using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SafetyScale.Application.Abstractions.Billing;
using SafetyScale.Domain.Entities;
using SafetyScale.Infrastructure.Authentication;
using SafetyScale.Infrastructure.Persistence;

namespace SafetyScale.Infrastructure.Billing;

public sealed class BillingService(
    ApplicationDbContext dbContext,
    IStripeBillingGateway stripeGateway,
    IOptions<PublicUrlsOptions> publicUrlsOptions,
    ILogger<BillingService> logger) : IBillingService
{
    private readonly PublicUrlsOptions _publicUrls = publicUrlsOptions.Value;

    public async Task<IReadOnlyList<BillingPlanDto>> ListAvailablePlansAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PlatformPlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.PriceMonthly)
            .Select(p => new BillingPlanDto(
                p.Id,
                p.Name,
                p.Code,
                p.Description,
                p.PriceMonthly,
                p.MaxSecurityGuards,
                p.MaxSectors,
                p.StripePriceId != null && p.StripePriceId != string.Empty))
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantBillingStatusDto?> GetTenantBillingStatusAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(t => t.PlatformPlan)
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant is null)
        {
            return null;
        }

        return MapTenantBillingStatus(tenant);
    }

    public async Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(
        Guid tenantId,
        string adminEmail,
        CreateCheckoutSessionInput input,
        CancellationToken cancellationToken = default)
    {
        if (!stripeGateway.IsConfigured)
        {
            return new CreateCheckoutSessionResult(CreateCheckoutSessionStatus.StripeNotConfigured);
        }

        var tenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant is null)
        {
            return new CreateCheckoutSessionResult(CreateCheckoutSessionStatus.TenantNotFound);
        }

        var plan = await dbContext.PlatformPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == input.PlanId, cancellationToken);

        if (plan is null)
        {
            return new CreateCheckoutSessionResult(CreateCheckoutSessionStatus.PlanNotFound);
        }

        if (!plan.IsActive)
        {
            return new CreateCheckoutSessionResult(CreateCheckoutSessionStatus.PlanInactive);
        }

        if (string.IsNullOrWhiteSpace(plan.StripePriceId))
        {
            return new CreateCheckoutSessionResult(CreateCheckoutSessionStatus.PlanNotConfigured);
        }

        var webBaseUrl = NormalizeWebBaseUrl(_publicUrls.WebBaseUrl);
        var metadata = new Dictionary<string, string>
        {
            ["tenant_id"] = tenantId.ToString(),
            ["plan_id"] = plan.Id.ToString(),
            ["plan_code"] = plan.Code,
        };

        try
        {
            var checkoutUrl = await stripeGateway.CreateCheckoutSessionAsync(
                new StripeCheckoutRequest(
                    tenant.StripeCustomerId,
                    adminEmail,
                    plan.StripePriceId,
                    $"{webBaseUrl}/app/billing?checkout=success",
                    $"{webBaseUrl}/app/billing?checkout=canceled",
                    metadata),
                cancellationToken);

            return new CreateCheckoutSessionResult(CreateCheckoutSessionStatus.Success, checkoutUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create Stripe Checkout Session for tenant {TenantId}.", tenantId);
            return new CreateCheckoutSessionResult(
                CreateCheckoutSessionStatus.StripeError,
                ErrorMessage: "Não foi possível iniciar o checkout. Tente novamente.");
        }
    }

    public async Task<CreatePortalSessionResult> CreatePortalSessionAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (!stripeGateway.IsConfigured)
        {
            return new CreatePortalSessionResult(CreatePortalSessionStatus.StripeNotConfigured);
        }

        var tenant = await dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant is null)
        {
            return new CreatePortalSessionResult(CreatePortalSessionStatus.TenantNotFound);
        }

        if (string.IsNullOrWhiteSpace(tenant.StripeCustomerId))
        {
            return new CreatePortalSessionResult(CreatePortalSessionStatus.NoStripeCustomer);
        }

        var webBaseUrl = NormalizeWebBaseUrl(_publicUrls.WebBaseUrl);

        try
        {
            var portalUrl = await stripeGateway.CreatePortalSessionAsync(
                tenant.StripeCustomerId,
                $"{webBaseUrl}/app/billing",
                cancellationToken);

            return new CreatePortalSessionResult(CreatePortalSessionStatus.Success, portalUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create Stripe Customer Portal session for tenant {TenantId}.", tenantId);
            return new CreatePortalSessionResult(
                CreatePortalSessionStatus.StripeError,
                ErrorMessage: "Não foi possível abrir o portal de assinatura.");
        }
    }

    public async Task<ProcessWebhookResult> ProcessWebhookAsync(
        string jsonPayload,
        string stripeSignatureHeader,
        CancellationToken cancellationToken = default)
    {
        var parsed = stripeGateway.ParseWebhookEvent(jsonPayload, stripeSignatureHeader);
        if (!parsed.IsValid || parsed.EventId is null || parsed.EventType is null)
        {
            return new ProcessWebhookResult(
                ProcessWebhookStatus.InvalidSignature,
                parsed.ErrorMessage ?? "Invalid Stripe webhook signature.");
        }

        var alreadyProcessed = await dbContext.StripeWebhookEvents
            .AnyAsync(e => e.StripeEventId == parsed.EventId, cancellationToken);

        if (alreadyProcessed)
        {
            return new ProcessWebhookResult(ProcessWebhookStatus.AlreadyProcessed);
        }

        if (parsed.EventData is null)
        {
            dbContext.StripeWebhookEvents.Add(new StripeWebhookEvent
            {
                Id = Guid.NewGuid(),
                StripeEventId = parsed.EventId,
                EventType = parsed.EventType,
                ProcessedAtUtc = DateTime.UtcNow,
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            return new ProcessWebhookResult(ProcessWebhookStatus.Ignored);
        }

        try
        {
            await ApplyWebhookEventAsync(parsed.EventType, parsed.EventData, cancellationToken);

            dbContext.StripeWebhookEvents.Add(new StripeWebhookEvent
            {
                Id = Guid.NewGuid(),
                StripeEventId = parsed.EventId,
                EventType = parsed.EventType,
                ProcessedAtUtc = DateTime.UtcNow,
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            return new ProcessWebhookResult(ProcessWebhookStatus.Success);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process Stripe webhook event {EventId}.", parsed.EventId);
            return new ProcessWebhookResult(
                ProcessWebhookStatus.ProcessingFailed,
                ex.Message);
        }
    }

    public async Task<LinkPlanStripeResult> LinkPlanStripeAsync(
        Guid planId,
        LinkPlanStripeInput input,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateStripeLinkInput(input);
        if (errors.Count > 0)
        {
            return new LinkPlanStripeResult(LinkPlanStripeStatus.ValidationFailed, errors);
        }

        var plan = await dbContext.PlatformPlans.FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);
        if (plan is null)
        {
            return new LinkPlanStripeResult(LinkPlanStripeStatus.NotFound);
        }

        plan.StripePriceId = input.StripePriceId.Trim();
        plan.StripeProductId = string.IsNullOrWhiteSpace(input.StripeProductId)
            ? null
            : input.StripeProductId.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        return new LinkPlanStripeResult(LinkPlanStripeStatus.Success);
    }

    private async Task ApplyWebhookEventAsync(
        string eventType,
        StripeWebhookEventData data,
        CancellationToken cancellationToken)
    {
        var tenant = await ResolveTenantAsync(data, cancellationToken);
        if (tenant is null)
        {
            logger.LogWarning("Stripe webhook {EventType} ignored: tenant not found.", eventType);
            return;
        }

        if (!string.IsNullOrWhiteSpace(data.CustomerId))
        {
            tenant.StripeCustomerId = data.CustomerId;
        }

        if (!string.IsNullOrWhiteSpace(data.SubscriptionId))
        {
            tenant.StripeSubscriptionId = data.SubscriptionId;
        }

        if (data.PlanId is Guid planId)
        {
            tenant.PlatformPlanId = planId;
        }

        if (data.CurrentPeriodEnd.HasValue)
        {
            tenant.CurrentPeriodEnd = data.CurrentPeriodEnd.Value;
        }

        if (!string.IsNullOrWhiteSpace(data.SubscriptionStatus))
        {
            tenant.BillingStatus = BillingStatusMapper.FromStripeSubscriptionStatus(data.SubscriptionStatus);
        }
        else if (eventType == EventTypes.CheckoutSessionCompleted)
        {
            tenant.BillingStatus = BillingStatus.Active;
        }
        else if (eventType == EventTypes.InvoicePaymentFailed)
        {
            tenant.BillingStatus = BillingStatus.PastDue;
        }
        else if (eventType == EventTypes.InvoicePaid)
        {
            tenant.BillingStatus = BillingStatus.Active;
        }

        tenant.LeadStatus = BillingStatusMapper.ResolveLeadStatus(tenant.BillingStatus, tenant.LeadStatus);

        if (BillingStatusMapper.IsSubscriptionActive(tenant.BillingStatus))
        {
            tenant.IsActive = true;
        }
        else if (tenant.BillingStatus is BillingStatus.Canceled or BillingStatus.Incomplete)
        {
            tenant.IsActive = false;
        }
    }

    private async Task<Tenant?> ResolveTenantAsync(
        StripeWebhookEventData data,
        CancellationToken cancellationToken)
    {
        if (data.TenantId is Guid tenantId)
        {
            return await dbContext.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(data.SubscriptionId))
        {
            return await dbContext.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.StripeSubscriptionId == data.SubscriptionId, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(data.CustomerId))
        {
            return await dbContext.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.StripeCustomerId == data.CustomerId, cancellationToken);
        }

        return null;
    }

    private static TenantBillingStatusDto MapTenantBillingStatus(Tenant tenant) =>
        new(
            tenant.Id,
            tenant.BillingStatus,
            tenant.LeadStatus,
            tenant.PlatformPlanId,
            tenant.PlatformPlan?.Name,
            tenant.CurrentPeriodEnd,
            BillingStatusMapper.IsSubscriptionActive(tenant.BillingStatus),
            !string.IsNullOrWhiteSpace(tenant.StripeCustomerId));

    private static string NormalizeWebBaseUrl(string webBaseUrl) =>
        webBaseUrl.TrimEnd('/');

    private static List<string> ValidateStripeLinkInput(LinkPlanStripeInput input)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(input.StripePriceId) || !input.StripePriceId.StartsWith("price_", StringComparison.Ordinal))
        {
            errors.Add("Informe um Stripe Price ID válido (prefixo price_).");
        }

        if (!string.IsNullOrWhiteSpace(input.StripeProductId) &&
            !input.StripeProductId.StartsWith("prod_", StringComparison.Ordinal))
        {
            errors.Add("O Stripe Product ID deve usar o prefixo prod_.");
        }

        return errors;
    }

    private static class EventTypes
    {
        public const string CheckoutSessionCompleted = "checkout.session.completed";
        public const string CustomerSubscriptionUpdated = "customer.subscription.updated";
        public const string CustomerSubscriptionDeleted = "customer.subscription.deleted";
        public const string InvoicePaymentFailed = "invoice.payment_failed";
        public const string InvoicePaid = "invoice.paid";
    }
}
