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
        var js = new FakeJsRuntime();
        var browserStorage = new BrowserSessionStorage(js, AppJsonSerializerOptions.Create());
        var sessionStorage = new JwtSessionStorage(browserStorage);
        var (authProvider, notificationCount) = AuthNotificationTracker.Create(sessionStorage);
        var navigation = new TestNavigationManager();

        await sessionStorage.SaveTokenAsync(
            JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?> { ["exp"] = JwtTestUtils.ExpSoon() }));

        using var client = CreateClient(
            sessionStorage,
            authProvider,
            navigation,
            HttpStatusCode.Unauthorized);

        using var request = CreateRequest(hadToken: true, skipAuthRedirect: false);
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        notificationCount().Should().Be(1);
        navigation.LastUri.Should().Be("/login");
        navigation.LastReplace.Should().BeTrue();
        js.Store.Should().NotContainKey(BrowserSessionStorage.AuthSessionStorageKey);
    }

    [Fact]
    public async Task SendAsync_On401WithoutHadToken_DoesNotClearOrRedirect()
    {
        var js = new FakeJsRuntime();
        var browserStorage = new BrowserSessionStorage(js, AppJsonSerializerOptions.Create());
        var sessionStorage = new JwtSessionStorage(browserStorage);
        var (authProvider, notificationCount) = AuthNotificationTracker.Create(sessionStorage);
        var navigation = new TestNavigationManager();

        using var client = CreateClient(
            sessionStorage,
            authProvider,
            navigation,
            HttpStatusCode.Unauthorized);

        using var request = CreateRequest(hadToken: false, skipAuthRedirect: false);
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        notificationCount().Should().Be(0);
        navigation.LastUri.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_On401WithSkipAuthRedirect_DoesNotClearOrRedirect()
    {
        var js = new FakeJsRuntime();
        var browserStorage = new BrowserSessionStorage(js, AppJsonSerializerOptions.Create());
        var sessionStorage = new JwtSessionStorage(browserStorage);
        var (authProvider, notificationCount) = AuthNotificationTracker.Create(sessionStorage);
        var navigation = new TestNavigationManager();

        await sessionStorage.SaveTokenAsync(
            JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?> { ["exp"] = JwtTestUtils.ExpSoon() }));

        using var client = CreateClient(
            sessionStorage,
            authProvider,
            navigation,
            HttpStatusCode.Unauthorized);

        using var request = CreateRequest(hadToken: true, skipAuthRedirect: true);
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        notificationCount().Should().Be(0);
        navigation.LastUri.Should().BeNull();
        js.Store.Should().ContainKey(BrowserSessionStorage.AuthSessionStorageKey);
    }

    [Fact]
    public async Task SendAsync_OnNon401_PassesThroughWithoutSideEffects()
    {
        var js = new FakeJsRuntime();
        var browserStorage = new BrowserSessionStorage(js, AppJsonSerializerOptions.Create());
        var sessionStorage = new JwtSessionStorage(browserStorage);
        var (authProvider, notificationCount) = AuthNotificationTracker.Create(sessionStorage);
        var navigation = new TestNavigationManager();

        await sessionStorage.SaveTokenAsync(
            JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?> { ["exp"] = JwtTestUtils.ExpSoon() }));

        using var client = CreateClient(
            sessionStorage,
            authProvider,
            navigation,
            HttpStatusCode.OK);

        using var request = CreateRequest(hadToken: true, skipAuthRedirect: false);
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        notificationCount().Should().Be(0);
        navigation.LastUri.Should().BeNull();
        js.Store.Should().ContainKey(BrowserSessionStorage.AuthSessionStorageKey);
    }

    private static HttpClient CreateClient(
        JwtSessionStorage sessionStorage,
        CustomAuthStateProvider authProvider,
        TestNavigationManager navigation,
        HttpStatusCode statusCode)
    {
        var handler = new UnauthorizedRedirectHandler(sessionStorage, authProvider, navigation)
        {
            InnerHandler = new StubHttpMessageHandler(statusCode),
        };

        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
    }

    private static HttpRequestMessage CreateRequest(bool hadToken, bool skipAuthRedirect)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
        request.Options.Set(ApiHttpContext.HadTokenKey, hadToken);
        request.Options.Set(ApiHttpContext.SkipAuthRedirectKey, skipAuthRedirect);
        return request;
    }
}
