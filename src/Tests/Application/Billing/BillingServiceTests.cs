using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SafetyScale.Application.Abstractions.Billing;
using SafetyScale.Domain.Entities;
using SafetyScale.Infrastructure.Authentication;
using SafetyScale.Infrastructure.Billing;
using SafetyScale.Infrastructure.Persistence;

namespace SafetyScale.Tests.Application.Billing;

public class BillingServiceTests
{
    [Fact]
    public async Task LinkPlanStripeAsync_WithInvalidPrice_ReturnsValidationFailed()
    {
        await using var db = CreateDbContext();
        var planId = Guid.NewGuid();
        db.PlatformPlans.Add(CreatePlan(planId));
        await db.SaveChangesAsync();

        var service = CreateService(db, new FakeStripeGateway());

        var result = await service.LinkPlanStripeAsync(
            planId,
            new LinkPlanStripeInput("invalid", null));

        result.Status.Should().Be(LinkPlanStripeStatus.ValidationFailed);
    }

    [Fact]
    public async Task LinkPlanStripeAsync_WithValidPrice_PersistsIds()
    {
        await using var db = CreateDbContext();
        var planId = Guid.NewGuid();
        db.PlatformPlans.Add(CreatePlan(planId));
        await db.SaveChangesAsync();

        var service = CreateService(db, new FakeStripeGateway());

        var result = await service.LinkPlanStripeAsync(
            planId,
            new LinkPlanStripeInput("price_test123", "prod_test123"));

        result.Status.Should().Be(LinkPlanStripeStatus.Success);

        var plan = await db.PlatformPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
        plan.StripePriceId.Should().Be("price_test123");
        plan.StripeProductId.Should().Be("prod_test123");
    }

    [Fact]
    public async Task ProcessWebhookAsync_WhenAlreadyProcessed_ReturnsAlreadyProcessed()
    {
        await using var db = CreateDbContext();
        db.StripeWebhookEvents.Add(new StripeWebhookEvent
        {
            Id = Guid.NewGuid(),
            StripeEventId = "evt_123",
            EventType = "checkout.session.completed",
            ProcessedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var gateway = new FakeStripeGateway
        {
            ParseResult = new StripeWebhookParseResult(
                true,
                "evt_123",
                "checkout.session.completed"),
        };

        var service = CreateService(db, gateway);
        var result = await service.ProcessWebhookAsync("{}", "sig");

        result.Status.Should().Be(ProcessWebhookStatus.AlreadyProcessed);
    }

    [Fact]
    public async Task ProcessWebhookAsync_CheckoutCompleted_UpdatesTenantBilling()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        db.PlatformPlans.Add(CreatePlan(planId));
        db.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Acme",
            Slug = "acme",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var gateway = new FakeStripeGateway
        {
            ParseResult = new StripeWebhookParseResult(
                true,
                "evt_new",
                "checkout.session.completed",
                new StripeWebhookEventData(
                    "cus_123",
                    "sub_123",
                    null,
                    null,
                    tenantId,
                    planId,
                    "cs_123")),
        };

        var service = CreateService(db, gateway);
        var result = await service.ProcessWebhookAsync("{}", "sig");

        result.Status.Should().Be(ProcessWebhookStatus.Success);

        var tenant = await db.Tenants.AsNoTracking().IgnoreQueryFilters().SingleAsync(t => t.Id == tenantId);
        tenant.StripeCustomerId.Should().Be("cus_123");
        tenant.StripeSubscriptionId.Should().Be("sub_123");
        tenant.PlatformPlanId.Should().Be(planId);
        tenant.BillingStatus.Should().Be(BillingStatus.Active);
        tenant.LeadStatus.Should().Be(LeadStatus.Contracted);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_WhenStripeNotConfigured_ReturnsStripeNotConfigured()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var plan = CreatePlan(planId);
        plan.StripePriceId = "price_abc";
        db.PlatformPlans.Add(plan);
        db.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Acme",
            Slug = "acme",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, new FakeStripeGateway { IsConfigured = false });
        var result = await service.CreateCheckoutSessionAsync(
            tenantId,
            "admin@test.local",
            new CreateCheckoutSessionInput(planId));

        result.Status.Should().Be(CreateCheckoutSessionStatus.StripeNotConfigured);
    }

    private static BillingService CreateService(ApplicationDbContext db, FakeStripeGateway gateway) =>
        new(
            db,
            gateway,
            Options.Create(new PublicUrlsOptions { WebBaseUrl = "http://localhost:4864" }),
            NullLogger<BillingService>.Instance);

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"billing-tests-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options, new BypassTenantExecutionContext());
    }

    private static PlatformPlan CreatePlan(Guid planId) =>
        new()
        {
            Id = planId,
            Name = "Starter",
            Code = "starter",
            PriceMonthly = 99.90m,
            MaxSecurityGuards = 10,
            MaxSectors = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

    private sealed class FakeStripeGateway : IStripeBillingGateway
    {
        public bool IsConfigured { get; init; } = true;

        public StripeWebhookParseResult ParseResult { get; init; } =
            new(false, ErrorMessage: "not configured");

        public Task<string> CreateCheckoutSessionAsync(
            StripeCheckoutRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult("https://checkout.stripe.test/session");

        public Task<string> CreatePortalSessionAsync(
            string stripeCustomerId,
            string returnUrl,
            CancellationToken cancellationToken = default) =>
            Task.FromResult("https://billing.stripe.test/portal");

        public StripeWebhookParseResult ParseWebhookEvent(string jsonPayload, string stripeSignatureHeader) =>
            ParseResult;
    }
}
