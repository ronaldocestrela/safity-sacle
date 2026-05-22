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
    public async Task<string?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        var passwordIsValid = await userManager.CheckPasswordAsync(user, password);
        if (!passwordIsValid)
        {
            return null;
        }

        // Users without a tenant cannot access tenant-scoped data.
        if (user.TenantId == Guid.Empty)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        return GenerateJwtToken(user, roles);
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
