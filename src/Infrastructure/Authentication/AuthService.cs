using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SafetyScale.Application.Abstractions.Authentication;
using SafetyScale.Infrastructure.Identity;

namespace SafetyScale.Infrastructure.Authentication;

public class AuthService(
    UserManager<AppUser> userManager,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    public Task<LoginResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default) =>
        LoginInternalAsync(email, password, UserKind.Tenant, includeTenantClaim: true, cancellationToken);

    public Task<LoginResult> PlatformLoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default) =>
        LoginInternalAsync(email, password, UserKind.Platform, includeTenantClaim: false, cancellationToken);

    private async Task<LoginResult> LoginInternalAsync(
        string email,
        string password,
        UserKind expectedKind,
        bool includeTenantClaim,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || user.UserKind != expectedKind)
        {
            return new LoginResult(LoginResultStatus.InvalidCredentials);
        }

        var passwordIsValid = await userManager.CheckPasswordAsync(user, password);
        if (!passwordIsValid)
        {
            return new LoginResult(LoginResultStatus.InvalidCredentials);
        }

        if (expectedKind == UserKind.Tenant)
        {
            if (user.TenantId is null || user.TenantId == Guid.Empty)
            {
                return new LoginResult(LoginResultStatus.InvalidCredentials);
            }

            if (!user.EmailConfirmed)
            {
                return new LoginResult(LoginResultStatus.EmailNotConfirmed);
            }
        }
        else if (!user.EmailConfirmed)
        {
            return new LoginResult(LoginResultStatus.EmailNotConfirmed);
        }

        var roles = await userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles, includeTenantClaim);
        return new LoginResult(LoginResultStatus.Success, token);
    }

    private string GenerateJwtToken(AppUser user, IEnumerable<string> roles, bool includeTenantClaim)
    {
        var options = jwtOptions.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(AuthClaimTypes.UserKind, user.UserKind.ToString()),
        };

        if (includeTenantClaim && user.TenantId is not null)
        {
            claims.Add(new Claim(TenantClaimTypes.TenantId, user.TenantId.Value.ToString()));
        }

        if (user.SecurityGuardId is not null)
        {
            claims.Add(new Claim(AuthClaimTypes.SecurityGuardId, user.SecurityGuardId.Value.ToString()));
        }

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(options.ExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
