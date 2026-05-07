using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafetyScale.Api.Contracts.UnavailableDays;
using SafetyScale.Application.UnavailableDays.Commands.AddUnavailableDay;
using SafetyScale.Application.UnavailableDays.Commands.RemoveUnavailableDay;
using SafetyScale.Application.UnavailableDays.Queries.GetUnavailableDays;

namespace SafetyScale.Api.Controllers;

[ApiController]
public sealed class UnavailableDaysController(ISender sender) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpPost("~/api/security-guards/{guardId:guid}/unavailable-days")]
    public async Task<IActionResult> AddForGuard(
        [FromRoute] Guid guardId,
        [FromBody] AddUnavailableDayRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddUnavailableDayCommand(guardId, request.Date, request.Reason),
            cancellationToken);

        return result.Status switch
        {
            AddUnavailableDayStatus.Success =>
                Created($"/api/unavailable-days/{result.Id!.Value}", new { id = result.Id }),
            AddUnavailableDayStatus.GuardNotFound => NotFound(),
            AddUnavailableDayStatus.GuardInactive => BadRequest(),
            AddUnavailableDayStatus.DuplicateDate => Conflict(),
            _ => throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, null),
        };
    }

    [Authorize(Roles = "Admin,Supervisor")]
    [HttpGet("~/api/security-guards/{guardId:guid}/unavailable-days")]
    public async Task<IActionResult> ListForGuard(
        [FromRoute] Guid guardId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUnavailableDaysQuery(guardId), cancellationToken);

        if (!result.GuardExists)
        {
            return NotFound();
        }

        return Ok(result.Items);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("~/api/unavailable-days/{id:guid}")]
    public async Task<IActionResult> Remove(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var removed = await sender.Send(new RemoveUnavailableDayCommand(id), cancellationToken);
        return removed ? NoContent() : NotFound();
    }
}
