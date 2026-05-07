using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafetyScale.Api.Contracts.Schedules;
using SafetyScale.Application.Schedules.Commands.GenerateMonthlySchedule;

namespace SafetyScale.Api.Controllers;

[ApiController]
[Route("api/schedules")]
public sealed class SchedulesController(ISender sender) : ControllerBase
{
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
