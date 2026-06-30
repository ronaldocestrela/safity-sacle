using SafetyScale.Application.Abstractions.Tenancy;

namespace SafetyScale.Tests.Application.Common;

public sealed class FakePlanLimitEvaluator : IPlanLimitEvaluator
{
    public PlanLimitEvaluation CreateSecurityGuardResult { get; init; } = PlanLimitEvaluation.Allowed();

    public PlanLimitEvaluation CreateSectorResult { get; init; } = PlanLimitEvaluation.Allowed();

    public PlanLimitEvaluation PlanAssignmentResult { get; init; } = PlanLimitEvaluation.Allowed();

    public Task<PlanLimitEvaluation> EvaluateCreateSecurityGuardAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateSecurityGuardResult);

    public Task<PlanLimitEvaluation> EvaluateCreateSectorAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateSectorResult);

    public Task<PlanLimitEvaluation> EvaluatePlanAssignmentAsync(
        Guid tenantId,
        Guid? newPlanId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(PlanAssignmentResult);
}
