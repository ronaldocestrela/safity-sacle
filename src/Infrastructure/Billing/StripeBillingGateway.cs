using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SafetyScale.Application.Abstractions.Billing;
using Stripe;
using Stripe.Checkout;

namespace SafetyScale.Infrastructure.Billing;

public sealed class StripeBillingGateway(
    IOptions<StripeOptions> options,
    ILogger<StripeBillingGateway> logger) : IStripeBillingGateway
{
    private readonly StripeOptions _options = options.Value;

    public bool IsConfigured => _options.IsConfigured;

    public async Task<string> CreateCheckoutSessionAsync(
        StripeCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        StripeClientConfigurator.Apply(_options);

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "subscription",
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = request.StripePriceId,
                    Quantity = 1,
                },
            ],
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            Metadata = request.Metadata.ToDictionary(static x => x.Key, static x => x.Value),
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = request.Metadata.ToDictionary(static x => x.Key, static x => x.Value),
            },
        };

        if (!string.IsNullOrWhiteSpace(request.StripeCustomerId))
        {
            sessionOptions.Customer = request.StripeCustomerId;
        }
        else
        {
            sessionOptions.CustomerEmail = request.CustomerEmail;
        }

        var service = new SessionService();
        var session = await service.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException("Stripe Checkout Session did not return a URL.");
        }

        return session.Url;
    }

    public async Task<string> CreatePortalSessionAsync(
        string stripeCustomerId,
        string returnUrl,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        StripeClientConfigurator.Apply(_options);

        var portalOptions = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = stripeCustomerId,
            ReturnUrl = returnUrl,
        };

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(portalOptions, cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException("Stripe Customer Portal session did not return a URL.");
        }

        return session.Url;
    }

    public StripeWebhookParseResult ParseWebhookEvent(string jsonPayload, string stripeSignatureHeader)
    {
        if (!_options.IsConfigured)
        {
            return new StripeWebhookParseResult(false, ErrorMessage: "Stripe is not configured.");
        }

        StripeClientConfigurator.Apply(_options);

        var receivedApiVersion = StripeWebhookDiagnostics.TryExtractApiVersion(jsonPayload);
        var eventIdHint = StripeWebhookDiagnostics.TryExtractEventId(jsonPayload);
        var eventTypeHint = StripeWebhookDiagnostics.TryExtractEventType(jsonPayload);

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                jsonPayload,
                stripeSignatureHeader,
                _options.WebhookSecret);

            return stripeEvent.Type switch
            {
                EventTypes.CheckoutSessionCompleted =>
                    ParseCheckoutSessionCompleted(stripeEvent),

                EventTypes.CustomerSubscriptionUpdated or EventTypes.CustomerSubscriptionDeleted =>
                    ParseSubscriptionEvent(stripeEvent),

                EventTypes.InvoicePaymentFailed or EventTypes.InvoicePaid =>
                    ParseInvoiceEvent(stripeEvent),

                _ => new StripeWebhookParseResult(
                    true,
                    stripeEvent.Id,
                    stripeEvent.Type,
                    EventData: null),
            };
        }
        catch (StripeException ex)
        {
            logger.LogWarning(
                ex,
                "Stripe webhook validation failed. EventId={EventId} EventType={EventType} ReceivedApiVersion={ReceivedApiVersion} ExpectedApiVersion={ExpectedApiVersion}",
                eventIdHint,
                eventTypeHint,
                receivedApiVersion,
                StripeClientConfigurator.PinnedApiVersion);

            return new StripeWebhookParseResult(false, ErrorMessage: ex.Message);
        }
    }

    private static StripeWebhookParseResult ParseCheckoutSessionCompleted(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is not Session session)
        {
            return new StripeWebhookParseResult(
                true,
                stripeEvent.Id,
                stripeEvent.Type,
                EventData: null);
        }

        var metadata = session.Metadata ?? new Dictionary<string, string>();
        metadata.TryGetValue("tenant_id", out var tenantRaw);
        metadata.TryGetValue("plan_id", out var planRaw);

        return new StripeWebhookParseResult(
            true,
            stripeEvent.Id,
            stripeEvent.Type,
            new StripeWebhookEventData(
                session.CustomerId,
                session.SubscriptionId,
                SubscriptionStatus: null,
                CurrentPeriodEnd: null,
                Guid.TryParse(tenantRaw, out var tenantId) ? tenantId : null,
                Guid.TryParse(planRaw, out var planId) ? planId : null,
                session.Id));
    }

    private static StripeWebhookParseResult ParseSubscriptionEvent(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is not Subscription subscription)
        {
            return new StripeWebhookParseResult(
                true,
                stripeEvent.Id,
                stripeEvent.Type,
                EventData: null);
        }

        var metadata = subscription.Metadata ?? new Dictionary<string, string>();
        metadata.TryGetValue("tenant_id", out var tenantRaw);
        metadata.TryGetValue("plan_id", out var planRaw);

        var periodEnd = subscription.Items?.Data is { Count: > 0 } items
            ? items.Max(static item => item.CurrentPeriodEnd)
            : (DateTime?)null;

        return new StripeWebhookParseResult(
            true,
            stripeEvent.Id,
            stripeEvent.Type,
            new StripeWebhookEventData(
                subscription.CustomerId,
                subscription.Id,
                subscription.Status,
                periodEnd,
                Guid.TryParse(tenantRaw, out var tenantId) ? tenantId : null,
                Guid.TryParse(planRaw, out var planId) ? planId : null,
                CheckoutSessionId: null));
    }

    private static StripeWebhookParseResult ParseInvoiceEvent(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is not Invoice invoice)
        {
            return new StripeWebhookParseResult(
                true,
                stripeEvent.Id,
                stripeEvent.Type,
                EventData: null);
        }

        return new StripeWebhookParseResult(
            true,
            stripeEvent.Id,
            stripeEvent.Type,
            new StripeWebhookEventData(
                invoice.CustomerId,
                SubscriptionId: null,
                SubscriptionStatus: stripeEvent.Type == EventTypes.InvoicePaymentFailed ? "past_due" : "active",
                CurrentPeriodEnd: null,
                TenantId: null,
                PlanId: null,
                CheckoutSessionId: null));
    }

    private void EnsureConfigured()
    {
        if (!_options.IsConfigured)
        {
            if (!StripeOptions.IsValidSecretKey(_options.SecretKey))
            {
                throw new InvalidOperationException(
                    "Stripe secret key is invalid. Use sk_ or rk_ key on the server, never pk_.");
            }

            throw new InvalidOperationException("Stripe is not configured.");
        }
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
