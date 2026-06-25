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
    public async Task<LoginResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return new LoginResult(LoginResultStatus.InvalidCredentials);
        }

        var passwordIsValid = await userManager.CheckPasswordAsync(user, password);
        if (!passwordIsValid)
        {
            return new LoginResult(LoginResultStatus.InvalidCredentials);
        }

        if (user.TenantId == Guid.Empty)
        {
            return new LoginResult(LoginResultStatus.InvalidCredentials);
        }

        if (!user.EmailConfirmed)
        {
            return new LoginResult(LoginResultStatus.EmailNotConfirmed);
        }

        var roles = await userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles);
        return new LoginResult(LoginResultStatus.Success, token);
    }

    private string GenerateJwtToken(AppUser user, IEnumerable<string> roles)
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
            new(TenantClaimTypes.TenantId, user.TenantId.ToString())
        };

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
