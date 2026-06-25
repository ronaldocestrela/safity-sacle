using System.Net;
using FluentAssertions;
using SafetyScale.Tests.Web.Blazor.TestHelpers;
using SafetyScale.Web.Blazor.Services.Api;
using SafetyScale.Web.Blazor.Services.Auth;

namespace SafetyScale.Tests.Web.Blazor.Api;

public sealed class UnauthorizedRedirectHandlerTests
{
    [Fact]
    public async Task SendAsync_On401WithHadToken_ClearsSessionNotifiesAndRedirects()
    {
        var authStack = BlazorAuthTestFactory.CreateAuthStack();
        var notificationCount = TrackNotifications(authStack.AuthProvider);

        await authStack.TenantSessionStorage.SaveTokenAsync(
            JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?> { ["exp"] = JwtTestUtils.ExpSoon() }));

        using var client = CreateClient(authStack, HttpStatusCode.Unauthorized);

        using var request = CreateRequest(hadToken: true, skipAuthRedirect: false, usesPlatformToken: false);
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        notificationCount().Should().Be(1);
        authStack.Navigation.LastUri.Should().Be("/login?reason=session-expired");
        authStack.Navigation.LastReplace.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_On401WithoutHadToken_DoesNotClearOrRedirect()
    {
        var authStack = BlazorAuthTestFactory.CreateAuthStack();
        var notificationCount = TrackNotifications(authStack.AuthProvider);

        using var client = CreateClient(authStack, HttpStatusCode.Unauthorized);

        using var request = CreateRequest(hadToken: false, skipAuthRedirect: false, usesPlatformToken: false);
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        notificationCount().Should().Be(0);
        authStack.Navigation.LastUri.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_On401WithSkipAuthRedirect_DoesNotClearOrRedirect()
    {
        var authStack = BlazorAuthTestFactory.CreateAuthStack();
        var notificationCount = TrackNotifications(authStack.AuthProvider);

        await authStack.TenantSessionStorage.SaveTokenAsync(
            JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?> { ["exp"] = JwtTestUtils.ExpSoon() }));

        using var client = CreateClient(authStack, HttpStatusCode.Unauthorized);

        using var request = CreateRequest(hadToken: true, skipAuthRedirect: true, usesPlatformToken: false);
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        notificationCount().Should().Be(0);
        authStack.Navigation.LastUri.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_OnNon401_PassesThroughWithoutSideEffects()
    {
        var authStack = BlazorAuthTestFactory.CreateAuthStack();
        var notificationCount = TrackNotifications(authStack.AuthProvider);

        await authStack.TenantSessionStorage.SaveTokenAsync(
            JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?> { ["exp"] = JwtTestUtils.ExpSoon() }));

        using var client = CreateClient(authStack, HttpStatusCode.OK);

        using var request = CreateRequest(hadToken: true, skipAuthRedirect: false, usesPlatformToken: false);
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        notificationCount().Should().Be(0);
        authStack.Navigation.LastUri.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_On401WithPlatformToken_RedirectsToPlatformLogin()
    {
        var authStack = BlazorAuthTestFactory.CreateAuthStack("/platform/tenants");
        var notificationCount = TrackNotifications(authStack.AuthProvider);

        await authStack.PlatformSessionStorage.SaveTokenAsync(
            JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?>
            {
                ["exp"] = JwtTestUtils.ExpSoon(),
                ["user_kind"] = "Platform",
                ["role"] = "PlatformOwner",
            }));

        using var client = CreateClient(authStack, HttpStatusCode.Unauthorized);

        using var request = CreateRequest(hadToken: true, skipAuthRedirect: false, usesPlatformToken: true);
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        notificationCount().Should().Be(1);
        authStack.Navigation.LastUri.Should().Be("/platform/login?reason=session-expired");
    }

    private static Func<int> TrackNotifications(CustomAuthStateProvider authProvider)
    {
        var count = 0;
        authProvider.AuthenticationStateChanged += _ => count++;
        return () => count;
    }

    private static HttpClient CreateClient(
        BlazorAuthTestFactory.AuthStack authStack,
        HttpStatusCode statusCode)
    {
        var handler = new UnauthorizedRedirectHandler(
            authStack.TenantSessionStorage,
            authStack.PlatformSessionStorage,
            authStack.AuthProvider,
            authStack.Navigation)
        {
            InnerHandler = new StubHttpMessageHandler(statusCode),
        };

        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
    }

    private static HttpRequestMessage CreateRequest(
        bool hadToken,
        bool skipAuthRedirect,
        bool usesPlatformToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
        request.Options.Set(ApiHttpContext.HadTokenKey, hadToken);
        request.Options.Set(ApiHttpContext.SkipAuthRedirectKey, skipAuthRedirect);
        request.Options.Set(ApiHttpContext.UsesPlatformTokenKey, usesPlatformToken);
        return request;
    }
}
