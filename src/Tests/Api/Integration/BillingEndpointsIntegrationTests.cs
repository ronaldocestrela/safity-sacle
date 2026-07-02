using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace SafetyScale.Tests.Api.Integration;

public class BillingEndpointsIntegrationTests
{
    [Fact]
    public async Task Billing_ListPlans_AsAdmin_ReturnsOk()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/billing/plans");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Billing_GetStatus_AsAdmin_ReturnsOk()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/billing/status");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Billing_CreateCheckout_WhenStripeNotConfigured_ReturnsServiceUnavailable()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);
        await AuthTestHelper.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/billing/checkout-session",
            new { planId = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task StripeWebhook_WithInvalidSignature_ReturnsBadRequest()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateHttpsClient(factory);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/stripe/webhook")
        {
            Content = new StringContent("{}"),
        };
        request.Headers.Add("Stripe-Signature", "invalid");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PlatformPlans_LinkStripe_AsPlatformOwner_ReturnsNoContent()
    {
        using var factory = new TestWebApplicationFactory();
        await PlatformTestHelper.EnsurePlatformUsersAsync(factory);
        using var client = PlatformTestHelper.CreateHttpsClient(factory);
        await PlatformTestHelper.AuthenticateAsPlatformUserAsync(
            client,
            "platform.owner@test.local",
            "Platform@12345");

        var code = $"billing-{Guid.NewGuid():N}"[..18];
        var create = await client.PostAsJsonAsync("/api/platform/plans", new
        {
            name = "Billing Plan",
            code,
            description = "Test",
            priceMonthly = 49.90m,
            maxSecurityGuards = 5,
            maxSectors = 2,
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var plans = await client.GetFromJsonAsync<List<PlatformPlanListItem>>("/api/platform/plans");
        var plan = plans!.Single(p => p.Code == code);

        var link = await client.PatchAsJsonAsync(
            $"/api/platform/plans/{plan.Id}/stripe",
            new { stripePriceId = "price_test123", stripeProductId = "prod_test123" });

        link.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static HttpClient CreateHttpsClient(TestWebApplicationFactory factory) =>
        factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

    private sealed record PlatformPlanListItem(
        Guid Id,
        string Code,
        string Name,
        decimal PriceMonthly,
        int MaxSecurityGuards,
        int MaxSectors,
        bool IsActive,
        DateTime CreatedAt,
        string? StripeProductId,
        string? StripePriceId);
}
