using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafetyScale.Api.Contracts.Auth;
using SafetyScale.Application.Abstractions.Authentication;

namespace SafetyScale.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuthService authService,
    IEmailConfirmationService emailConfirmationService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request.Email, request.Password, cancellationToken);

        return result.Status switch
        {
            LoginResultStatus.Success => Ok(new { token = result.Token }),
            LoginResultStatus.EmailNotConfirmed => Unauthorized(new
            {
                message = "Confirme seu e-mail antes de entrar.",
                code = "email_not_confirmed",
            }),
            _ => Unauthorized(new { message = "Invalid credentials." }),
        };
    }

    [AllowAnonymous]
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request,
        CancellationToken cancellationToken)
    {
        var result = await emailConfirmationService.ConfirmAsync(
            request.UserId,
            request.Token,
            cancellationToken);

        return result.Status switch
        {
            ConfirmEmailStatus.Success => Ok(new { message = "E-mail confirmado com sucesso." }),
            ConfirmEmailStatus.AlreadyConfirmed => Ok(new { message = "E-mail já estava confirmado." }),
            ConfirmEmailStatus.InvalidToken => BadRequest(new { message = "Link de confirmação inválido ou expirado." }),
            ConfirmEmailStatus.UserNotFound => NotFound(new { message = "Usuário não encontrado." }),
            _ => BadRequest(new { message = "Não foi possível confirmar o e-mail." }),
        };
    }
}
