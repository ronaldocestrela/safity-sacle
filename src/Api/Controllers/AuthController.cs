using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafetyScale.Api.Contracts.Auth;
using SafetyScale.Application.Abstractions.Authentication;

namespace SafetyScale.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var token = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
        if (token is null)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        return Ok(new { token });
    }
}
