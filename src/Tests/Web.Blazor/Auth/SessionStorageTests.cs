using FluentAssertions;
using SafetyScale.Tests.Web.Blazor.TestHelpers;
using SafetyScale.Web.Blazor.Models.Auth;
using SafetyScale.Web.Blazor.Services.Api;
using SafetyScale.Web.Blazor.Services.Auth;

namespace SafetyScale.Tests.Web.Blazor.Auth;

public sealed class BrowserSessionStorageTests
{
    [Fact]
    public async Task GetTokenAsync_ReturnsNullWhenEmpty()
    {
        var storage = CreateStorage(new FakeJsRuntime());

        var token = await storage.GetTokenAsync();

        token.Should().BeNull();
    }

    [Fact]
    public async Task SaveAndGetTokenAsync_RoundTripsCamelCaseJson()
    {
        var js = new FakeJsRuntime();
        var storage = CreateStorage(js);
        const string token = "header.payload.sig";

        await storage.SaveTokenAsync(token);
        var readBack = await storage.GetTokenAsync();

        readBack.Should().Be(token);
        js.Store.Should().ContainKey(BrowserSessionStorage.AuthSessionStorageKey);
        js.Store[BrowserSessionStorage.AuthSessionStorageKey].Should().Be("""{"token":"header.payload.sig"}""");
    }

    [Fact]
    public async Task GetTokenAsync_ReturnsNullForInvalidJson()
    {
        var js = new FakeJsRuntime();
        js.SetRaw(BrowserSessionStorage.AuthSessionStorageKey, "{not-json");
        var storage = CreateStorage(js);

        var token = await storage.GetTokenAsync();

        token.Should().BeNull();
    }

    [Fact]
    public async Task GetTokenAsync_ReadsReactWrittenTokenShape()
    {
        var js = new FakeJsRuntime();
        js.SetRaw(BrowserSessionStorage.AuthSessionStorageKey, """{"token":"react.token.here"}""");
        var storage = CreateStorage(js);

        var token = await storage.GetTokenAsync();

        token.Should().Be("react.token.here");
    }

    [Fact]
    public async Task ClearAsync_RemovesSessionKey()
    {
        var js = new FakeJsRuntime();
        var storage = CreateStorage(js);
        await storage.SaveTokenAsync("x.y.z");

        await storage.ClearAsync();

        js.Store.Should().NotContainKey(BrowserSessionStorage.AuthSessionStorageKey);
        (await storage.GetTokenAsync()).Should().BeNull();
    }

    private static BrowserSessionStorage CreateStorage(FakeJsRuntime jsRuntime) =>
        new(jsRuntime, AppJsonSerializerOptions.Create());
}

public sealed class JwtSessionStorageTests
{
    [Fact]
    public async Task GetSessionAsync_ReturnsNullWhenEmpty()
    {
        var sessionStorage = CreateSessionStorage(new FakeJsRuntime());

        var session = await sessionStorage.GetSessionAsync();

        session.Should().BeNull();
    }

    [Fact]
    public async Task SaveTokenAsync_RoundTripsValidToken()
    {
        var js = new FakeJsRuntime();
        var sessionStorage = CreateSessionStorage(js);
        var token = JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?>
        {
            ["exp"] = JwtTestUtils.ExpSoon(),
            ["email"] = "a@b.com",
            ["role"] = "Admin",
        });

        var saved = await sessionStorage.SaveTokenAsync(token);
        var loaded = await sessionStorage.GetSessionAsync();

        saved.Should().NotBeNull();
        saved!.Email.Should().Be("a@b.com");
        saved.Roles.Should().Equal(UserRole.Admin);
        saved.TenantId.Should().Be(JwtTestUtils.DefaultTestTenantId);
        loaded.Should().NotBeNull();
        loaded!.Token.Should().Be(token);
    }

    [Fact]
    public async Task GetSessionAsync_ClearsExpiredTokenOnLoad()
    {
        var js = new FakeJsRuntime();
        var sessionStorage = CreateSessionStorage(js);
        var token = JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?>
        {
            ["exp"] = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds(),
            ["role"] = "Admin",
        });

        js.SetRaw(BrowserSessionStorage.AuthSessionStorageKey, $$"""{"token":"{{token}}"}""");

        var session = await sessionStorage.GetSessionAsync();

        session.Should().BeNull();
        js.Store.Should().NotContainKey(BrowserSessionStorage.AuthSessionStorageKey);
    }

    [Fact]
    public async Task SaveTokenAsync_ReturnsNullForInvalidTokenWithoutPersisting()
    {
        var js = new FakeJsRuntime();
        var sessionStorage = CreateSessionStorage(js);

        var saved = await sessionStorage.SaveTokenAsync("invalid-token");

        saved.Should().BeNull();
        js.Store.Should().NotContainKey(BrowserSessionStorage.AuthSessionStorageKey);
    }

    [Fact]
    public async Task ClearAsync_RemovesStoredSession()
    {
        var js = new FakeJsRuntime();
        var sessionStorage = CreateSessionStorage(js);
        var token = JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?>
        {
            ["exp"] = JwtTestUtils.ExpSoon(),
        });
        await sessionStorage.SaveTokenAsync(token);

        await sessionStorage.ClearAsync();

        js.Store.Should().NotContainKey(BrowserSessionStorage.AuthSessionStorageKey);
        (await sessionStorage.GetSessionAsync()).Should().BeNull();
    }

    private static JwtSessionStorage CreateSessionStorage(FakeJsRuntime jsRuntime)
    {
        var browserStorage = new BrowserSessionStorage(jsRuntime, AppJsonSerializerOptions.Create());
        return new JwtSessionStorage(browserStorage);
    }
}
