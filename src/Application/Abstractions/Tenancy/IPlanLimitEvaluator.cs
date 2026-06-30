namespace SafetyScale.Application.Abstractions.Tenancy;

public sealed record PlanLimitEvaluation(bool IsAllowed, string? ErrorMessage = null)
{
    public static PlanLimitEvaluation Allowed() => new(true);

    public static PlanLimitEvaluation Denied(string message) => new(false, message);
}

public interface IPlanLimitEvaluator
{
    Task<PlanLimitEvaluation> EvaluateCreateSecurityGuardAsync(CancellationToken cancellationToken = default);

    Task<PlanLimitEvaluation> EvaluateCreateSectorAsync(CancellationToken cancellationToken = default);

    Task<PlanLimitEvaluation> EvaluatePlanAssignmentAsync(
        Guid tenantId,
        Guid? newPlanId,
        CancellationToken cancellationToken = default);
}
