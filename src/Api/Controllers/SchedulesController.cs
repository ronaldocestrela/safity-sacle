using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafetyScale.Api.Contracts.Schedules;
using SafetyScale.Application.Schedules.Commands.GenerateMonthlySchedule;
using SafetyScale.Application.Schedules.Queries.GetMonthlySchedule;
using SafetyScale.Application.Schedules.Queries.GetMonthlySchedules;

namespace SafetyScale.Api.Controllers;

[ApiController]
[Route("api/schedules")]
public sealed class SchedulesController(ISender sender) : ControllerBase
{
    [Authorize(Roles = "Admin,Supervisor")]
    [HttpGet("month/{month:int}/year/{year:int}")]
    public async Task<IActionResult> GetByMonthYear(
        [FromRoute] int month,
        [FromRoute] int year,
        CancellationToken cancellationToken)
    {
        var schedule = await sender.Send(new GetMonthlySchedulesQuery(month, year), cancellationToken);
        return schedule is null ? NotFound() : Ok(schedule);
    }

    [Authorize(Roles = "Admin,Supervisor")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var schedule = await sender.Send(new GetMonthlyScheduleQuery(id), cancellationToken);
        return schedule is null ? NotFound() : Ok(schedule);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("generate")]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateMonthlyScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GenerateMonthlyScheduleCommand(request.Month, request.Year),
            cancellationToken);

        return result.Status switch
        {
            GenerateMonthlyScheduleStatus.Success =>
                Created($"/api/schedules/{result.ScheduleId!.Value}", new { id = result.ScheduleId }),
            GenerateMonthlyScheduleStatus.AlreadyExists => Conflict(),
            GenerateMonthlyScheduleStatus.NoActiveGuards => BadRequest(),
            GenerateMonthlyScheduleStatus.ImpossibleToGenerate => BadRequest(new { failedDate = result.FailedDate }),
            _ => throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, null),
        };
    }
}
