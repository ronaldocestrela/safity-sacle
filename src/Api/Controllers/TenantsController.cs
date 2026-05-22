using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafetyScale.Api.Contracts.Tenants;
using SafetyScale.Application.Abstractions.Tenancy;

namespace SafetyScale.Api.Controllers;

[ApiController]
[Route("api/tenants")]
public sealed class TenantsController(ITenantRegistrationService tenantRegistrationService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterTenantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterTenantRequest request,
        CancellationToken cancellationToken)
    {
        var input = new RegisterTenantInput(
            request.TenantName,
            request.AdminName,
            request.AdminEmail,
            request.AdminPassword,
            request.ConfirmPassword);

        var result = await tenantRegistrationService.RegisterAsync(input, cancellationToken);

        return result.Status switch
        {
            RegisterTenantStatus.Success =>
                Created(
                    $"/api/tenants/{result.TenantId!.Value}",
                    new RegisterTenantResponse(result.TenantId.Value, result.AdminUserId!, result.TenantSlug!)),

            RegisterTenantStatus.AdminEmailAlreadyExists =>
                Conflict(new { message = "Este e-mail já está cadastrado." }),

            RegisterTenantStatus.TenantSlugConflict =>
                Conflict(new
                {
                    message = "Não foi possível gerar um identificador único para a empresa. Tente novamente.",
                }),

            RegisterTenantStatus.InvalidPassword =>
                BadRequest(new { errors = result.Errors ?? Array.Empty<string>() }),

            RegisterTenantStatus.ValidationFailed =>
                BadRequest(new { errors = result.Errors ?? Array.Empty<string>() }),

            _ => throw new InvalidOperationException($"Unexpected tenant registration status {result.Status}."),
        };
    }
}
