using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafetyScale.Api.Authorization;
using SafetyScale.Api.Contracts.Platform;
using SafetyScale.Application.Abstractions.Tenancy;

namespace SafetyScale.Api.Controllers;

[ApiController]
[Route("api/platform/plans")]
[Authorize(Policy = AuthorizationPolicies.PlatformRead)]
public sealed class PlatformPlansController(IPlatformPlanService platformPlanService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PlatformPlanResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var plans = await platformPlanService.ListAsync(cancellationToken);
        return Ok(plans.Select(Map).ToList());
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(IReadOnlyList<PlatformPlanResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListActive(CancellationToken cancellationToken)
    {
        var plans = await platformPlanService.ListActiveAsync(cancellationToken);
        return Ok(plans.Select(Map).ToList());
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.PlatformManagement)]
    [ProducesResponseType(typeof(PlatformPlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePlatformPlanRequest request,
        CancellationToken cancellationToken)
    {
        var input = new CreatePlatformPlanInput(
            request.Name,
            request.Code,
            request.Description,
            request.PriceMonthly);

        var result = await platformPlanService.CreateAsync(input, cancellationToken);

        return result.Status switch
        {
            CreatePlatformPlanStatus.Success =>
                Created(
                    $"/api/platform/plans/{result.PlanId!.Value}",
                    new PlatformPlanResponse(
                        result.PlanId.Value,
                        request.Name.Trim(),
                        request.Code.Trim().ToLowerInvariant(),
                        string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                        request.PriceMonthly,
                        true,
                        DateTime.UtcNow)),

            CreatePlatformPlanStatus.CodeAlreadyExists =>
                Conflict(new { message = "Já existe um plano com este código." }),

            CreatePlatformPlanStatus.ValidationFailed =>
                BadRequest(new { errors = result.Errors ?? Array.Empty<string>() }),

            _ => throw new InvalidOperationException($"Unexpected platform plan status {result.Status}."),
        };
    }

    [HttpPut("{planId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.PlatformManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid planId,
        [FromBody] UpdatePlatformPlanRequest request,
        CancellationToken cancellationToken)
    {
        var input = new UpdatePlatformPlanInput(
            request.Name,
            request.Description,
            request.PriceMonthly);

        var result = await platformPlanService.UpdateAsync(planId, input, cancellationToken);

        return result.Status switch
        {
            UpdatePlatformPlanStatus.Success => NoContent(),
            UpdatePlatformPlanStatus.NotFound => NotFound(new { message = "Plano não encontrado." }),
            UpdatePlatformPlanStatus.ValidationFailed =>
                BadRequest(new { errors = result.Errors ?? Array.Empty<string>() }),
            _ => throw new InvalidOperationException($"Unexpected platform plan update status {result.Status}."),
        };
    }

    [HttpPatch("{planId:guid}/activate")]
    [Authorize(Policy = AuthorizationPolicies.PlatformManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid planId, CancellationToken cancellationToken)
    {
        var result = await platformPlanService.SetActiveAsync(planId, isActive: true, cancellationToken);
        return result.Status switch
        {
            SetPlatformPlanActiveStatus.Success => NoContent(),
            SetPlatformPlanActiveStatus.NotFound => NotFound(new { message = "Plano não encontrado." }),
            _ => throw new InvalidOperationException($"Unexpected platform plan active status {result.Status}."),
        };
    }

    [HttpPatch("{planId:guid}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.PlatformManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid planId, CancellationToken cancellationToken)
    {
        var result = await platformPlanService.SetActiveAsync(planId, isActive: false, cancellationToken);
        return result.Status switch
        {
            SetPlatformPlanActiveStatus.Success => NoContent(),
            SetPlatformPlanActiveStatus.NotFound => NotFound(new { message = "Plano não encontrado." }),
            _ => throw new InvalidOperationException($"Unexpected platform plan active status {result.Status}."),
        };
    }

    private static PlatformPlanResponse Map(PlatformPlanSummaryDto plan) =>
        new(
            plan.Id,
            plan.Name,
            plan.Code,
            plan.Description,
            plan.PriceMonthly,
            plan.IsActive,
            plan.CreatedAt);
}
