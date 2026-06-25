using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafetyScale.Api.Authorization;
using SafetyScale.Api.Contracts.Platform;
using SafetyScale.Application.Abstractions.Tenancy;

namespace SafetyScale.Api.Controllers;

[ApiController]
[Route("api/platform/tenants")]
[Authorize(Policy = AuthorizationPolicies.PlatformRead)]
public sealed class PlatformTenantsController(IPlatformTenantService platformTenantService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PlatformTenantResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var tenants = await platformTenantService.ListAsync(cancellationToken);
        var response = tenants
            .Select(t => new PlatformTenantResponse(t.Id, t.Name, t.Slug, t.IsActive, t.CreatedAt))
            .ToList();

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.PlatformManagement)]
    [ProducesResponseType(typeof(CreatePlatformTenantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePlatformTenantRequest request,
        CancellationToken cancellationToken)
    {
        var input = new CreatePlatformTenantInput(
            request.TenantName,
            request.AdminName,
            request.AdminEmail,
            request.AdminPassword);

        var result = await platformTenantService.CreateAsync(input, cancellationToken);

        return result.Status switch
        {
            CreatePlatformTenantStatus.Success =>
                Created(
                    $"/api/platform/tenants/{result.TenantId!.Value}",
                    new CreatePlatformTenantResponse(
                        result.TenantId.Value,
                        result.AdminUserId!,
                        result.TenantSlug!)),

            CreatePlatformTenantStatus.AdminEmailAlreadyExists =>
                Conflict(new { message = "Este e-mail já está cadastrado." }),

            CreatePlatformTenantStatus.TenantSlugConflict =>
                Conflict(new
                {
                    message = "Não foi possível gerar um identificador único para a empresa. Tente novamente.",
                }),

            CreatePlatformTenantStatus.InvalidPassword or CreatePlatformTenantStatus.ValidationFailed =>
                BadRequest(new { errors = result.Errors ?? Array.Empty<string>() }),

            _ => throw new InvalidOperationException($"Unexpected platform tenant status {result.Status}."),
        };
    }

    [HttpPatch("{tenantId:guid}/activate")]
    [Authorize(Policy = AuthorizationPolicies.PlatformManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid tenantId, CancellationToken cancellationToken)
    {
        var result = await platformTenantService.SetActiveAsync(tenantId, isActive: true, cancellationToken);
        return result.Status switch
        {
            SetTenantActiveStatus.Success => NoContent(),
            SetTenantActiveStatus.NotFound => NotFound(new { message = "Tenant não encontrado." }),
            _ => throw new InvalidOperationException($"Unexpected tenant active status {result.Status}."),
        };
    }

    [HttpPatch("{tenantId:guid}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.PlatformManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid tenantId, CancellationToken cancellationToken)
    {
        var result = await platformTenantService.SetActiveAsync(tenantId, isActive: false, cancellationToken);
        return result.Status switch
        {
            SetTenantActiveStatus.Success => NoContent(),
            SetTenantActiveStatus.NotFound => NotFound(new { message = "Tenant não encontrado." }),
            _ => throw new InvalidOperationException($"Unexpected tenant active status {result.Status}."),
        };
    }
}
