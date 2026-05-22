using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafetyScale.Api.Contracts.Sectors;
using SafetyScale.Application.Sectors.Commands.ActivateSector;
using SafetyScale.Application.Sectors.Commands.CreateSector;
using SafetyScale.Application.Sectors.Commands.InactivateSector;
using SafetyScale.Application.Sectors.Commands.UpdateSector;
using SafetyScale.Application.Sectors.Queries.GetSectors;

namespace SafetyScale.Api.Controllers;

[ApiController]
[Route("api/sectors")]
public sealed class SectorsController(ISender sender) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSectorRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            new CreateSectorCommand(request.Name, request.Description),
            cancellationToken);
        return Created($"/api/sectors/{id}", new { id });
    }

    [Authorize(Roles = "Admin,Supervisor")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var sectors = await sender.Send(new GetSectorsQuery(isActive), cancellationToken);
        return Ok(sectors);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateSectorRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await sender.Send(new UpdateSectorCommand(id, request.Name, request.Description), cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/inactive")]
    public async Task<IActionResult> Inactivate(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var done = await sender.Send(new InactivateSectorCommand(id), cancellationToken);
        return done ? NoContent() : NotFound();
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> Activate(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var done = await sender.Send(new ActivateSectorCommand(id), cancellationToken);
        return done ? NoContent() : NotFound();
    }
}
