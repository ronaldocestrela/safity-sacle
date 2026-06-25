using FluentAssertions;
using SafetyScale.Tests.Web.Blazor.TestHelpers;
using SafetyScale.Web.Blazor.Models.Auth;
using SafetyScale.Web.Blazor.Services.Auth;

namespace SafetyScale.Tests.Web.Blazor.Auth;

public sealed class JwtParserTests
{
    [Fact]
    public void ParseJwtPayload_ReadsSubAndEmail()
    {
        var token = JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?>
        {
            ["sub"] = "u1",
            ["email"] = "x@y.com",
            ["exp"] = JwtTestUtils.ExpSoon(),
        });

        var payload = JwtParser.ParseJwtPayload(token);

        payload.Should().NotBeNull();
        payload!.Value.GetProperty("sub").GetString().Should().Be("u1");
        JwtParser.EmailFromPayload(payload.Value).Should().Be("x@y.com");
    }

    [Fact]
    public void BuildSessionFromToken_FiltersRolesFromShortAndMsClaimKeys()
    {
        var adminToken = JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?>
        {
            ["role"] = "Admin",
            ["tenant_id"] = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            ["user_kind"] = "Tenant",
            ["exp"] = JwtTestUtils.ExpSoon(),
        });

        var adminSession = JwtParser.BuildSessionFromToken(adminToken);
        adminSession!.Roles.Should().Equal(UserRole.Admin);

        var supervisorToken = JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?>
        {
            ["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] =
                new[] { "Supervisor", "Extra" },
            ["tenant_id"] = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            ["user_kind"] = "Tenant",
            ["exp"] = JwtTestUtils.ExpSoon(),
        });

        var supervisorSession = JwtParser.BuildSessionFromToken(supervisorToken);
        supervisorSession!.Roles.Should().Equal(UserRole.Supervisor);
    }

    [Fact]
    public void IsJwtExpired_DetectsPastAndFutureExp()
    {
        var past = DateTimeOffset.UtcNow.AddSeconds(-10);
        var future = DateTimeOffset.UtcNow.AddHours(1);

        var pastPayload = JwtParser.ParseJwtPayload(
            JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?> { ["exp"] = past.ToUnixTimeSeconds() }));
        var futurePayload = JwtParser.ParseJwtPayload(
            JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?> { ["exp"] = future.ToUnixTimeSeconds() }));

        JwtParser.IsJwtExpired(pastPayload!.Value, past).Should().BeTrue();
        JwtParser.IsJwtExpired(futurePayload!.Value, past).Should().BeFalse();
    }

    [Fact]
    public void TenantIdFromPayload_ExtractsTenantClaim()
    {
        const string tenantId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
        var token = JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?>
        {
            ["tenant_id"] = tenantId,
            ["exp"] = JwtTestUtils.ExpSoon(),
        });

        var payload = JwtParser.ParseJwtPayload(token);

        JwtParser.TenantIdFromPayload(payload!.Value).Should().Be(tenantId);
    }

    [Fact]
    public void EmailFromPayload_UsesUniqueNameWhenEmailMissing()
    {
        var token = JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?>
        {
            ["unique_name"] = "user@example.com",
            ["exp"] = JwtTestUtils.ExpSoon(),
        });

        var payload = JwtParser.ParseJwtPayload(token);

        JwtParser.EmailFromPayload(payload!.Value).Should().Be("user@example.com");
    }

    [Fact]
    public void BuildSessionFromToken_ReturnsNullForMalformedToken()
    {
        JwtParser.BuildSessionFromToken("not-a-jwt").Should().BeNull();
        JwtParser.BuildSessionFromToken("header.not-base64url.sig").Should().BeNull();
    }

    [Fact]
    public void BuildSessionFromToken_ReturnsNullWhenExpiredOrMissingTenant()
    {
        var expiredToken = JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?>
        {
            ["exp"] = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds(),
            ["role"] = "Admin",
        });

        JwtParser.BuildSessionFromToken(expiredToken).Should().BeNull();

        var noTenantToken = JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?>
        {
            ["tenant_id"] = null,
            ["exp"] = JwtTestUtils.ExpSoon(),
        });

        JwtParser.BuildSessionFromToken(noTenantToken).Should().BeNull();
    }

    [Fact]
    public void BuildPlatformSessionFromToken_FiltersPlatformRoles()
    {
        var token = JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?>
        {
            ["role"] = "PlatformOwner",
            ["user_kind"] = "Platform",
            ["email"] = "owner@platform.local",
            ["exp"] = JwtTestUtils.ExpSoon(),
        });

        var session = JwtParser.BuildPlatformSessionFromToken(token);
        session.Should().NotBeNull();
        session!.Roles.Should().Equal(PlatformUserRole.PlatformOwner);
        session.Email.Should().Be("owner@platform.local");
    }

    [Fact]
    public void BuildSessionFromToken_ReturnsNullForPlatformToken()
    {
        var token = JwtTestUtils.MakeUnsignedJwt(new Dictionary<string, object?>
        {
            ["role"] = "PlatformOwner",
            ["user_kind"] = "Platform",
            ["exp"] = JwtTestUtils.ExpSoon(),
        });

        JwtParser.BuildSessionFromToken(token).Should().BeNull();
    }
}
