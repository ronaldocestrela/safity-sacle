using System.Globalization;
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
    [Authorize(Roles = "Admin,Supervisor,SecurityGuard")]
    [HttpGet("month/{month:int}/year/{year:int}")]
    public async Task<IActionResult> GetByMonthYear(
        [FromRoute] int month,
        [FromRoute] int year,
        CancellationToken cancellationToken)
    {
        var schedule = await sender.Send(new GetMonthlySchedulesQuery(month, year), cancellationToken);
        return schedule is null ? NotFound() : Ok(schedule);
    }

    [Authorize(Roles = "Admin,Supervisor,SecurityGuard")]
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
            GenerateMonthlyScheduleStatus.NoWorkloadSectorsConfigured => BadRequest(),
            GenerateMonthlyScheduleStatus.ImpossibleToGenerate => BadRequest(BuildCoverageFailure(result.FailedDate)),
            _ => throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, null),
        };
    }

    private static ScheduleCoverageFailureResponse BuildCoverageFailure(DateOnly? failedDate)
    {
        const string code = "ScheduleCoverageFailed";
        if (failedDate is null)
        {
            return new ScheduleCoverageFailureResponse
            {
                Code = code,
                Message =
                    "Não foi possível gerar a escala porque não há seguranças elegíveis suficientes para cobrir todas as vagas de um dos dias.",
                FailedDate = null,
            };
        }

        var display = failedDate.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        var message =
            $"Não foi possível gerar a escala para {display} porque não há seguranças elegíveis suficientes " +
            "para cobrir todas as vagas do dia. Verifique vagas por setor, vínculos dos seguranças aos setores e indisponibilidades.";

        return new ScheduleCoverageFailureResponse
        {
            Code = code,
            Message = message,
            FailedDate = failedDate,
        };
    }
}
