using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafetyScale.Api.Contracts.SecurityGuards;
using SafetyScale.Application.SecurityGuards.Commands.CreateSecurityGuard;
using SafetyScale.Application.SecurityGuards.Commands.ActivateSecurityGuard;
using SafetyScale.Application.SecurityGuards.Commands.InactivateSecurityGuard;
using SafetyScale.Application.SecurityGuards.Commands.SetSecurityGuardSectors;
using SafetyScale.Application.SecurityGuards.Commands.UpdateSecurityGuard;
using SafetyScale.Application.SecurityGuards.Queries.GetSecurityGuards;

namespace SafetyScale.Api.Controllers;

[ApiController]
[Route("api/security-guards")]
public class SecurityGuardsController(ISender sender) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSecurityGuardRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreateSecurityGuardCommand(request.Name), cancellationToken);
        return Created($"/api/security-guards/{id}", new { id });
    }

    [Authorize(Roles = "Admin,Supervisor")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var guards = await sender.Send(new GetSecurityGuardsQuery(isActive), cancellationToken);
        return Ok(guards);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateSecurityGuardRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new UpdateSecurityGuardCommand(id, request.Name), cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/inactive")]
    public async Task<IActionResult> Inactivate(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var inactivated = await sender.Send(new InactivateSecurityGuardCommand(id), cancellationToken);
        return inactivated ? NoContent() : NotFound();
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> Activate(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var activated = await sender.Send(new ActivateSecurityGuardCommand(id), cancellationToken);
        return activated ? NoContent() : NotFound();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}/sectors")]
    public async Task<IActionResult> SetSectors(
        [FromRoute] Guid id,
        [FromBody] UpdateSecurityGuardSectorsRequest request,
        CancellationToken cancellationToken)
    {
        var status = await sender.Send(
            new SetSecurityGuardSectorsCommand(id, request.SectorIds),
            cancellationToken);

        return status switch
        {
            SetSecurityGuardSectorsStatus.Success => NoContent(),
            SetSecurityGuardSectorsStatus.GuardNotFound => NotFound(),
            SetSecurityGuardSectorsStatus.InvalidSectors => BadRequest(
                new { error = "One or more sector ids are invalid or inactive." }),
            _ => Problem(),
        };
    }
}
