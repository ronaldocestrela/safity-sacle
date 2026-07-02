using SafetyScale.Domain.Entities;

namespace SafetyScale.Application.Abstractions.Tenancy;

public sealed record PlatformPlanSummaryDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    decimal PriceMonthly,
    int MaxSecurityGuards,
    int MaxSectors,
    bool IsActive,
    DateTime CreatedAt,
    string? StripeProductId,
    string? StripePriceId);

public sealed record CreatePlatformPlanInput(
    string Name,
    string Code,
    string? Description,
    decimal PriceMonthly,
    int MaxSecurityGuards,
    int MaxSectors);

public sealed record UpdatePlatformPlanInput(
    string Name,
    string? Description,
    decimal PriceMonthly,
    int MaxSecurityGuards,
    int MaxSectors);

public enum CreatePlatformPlanStatus
{
    Success,
    ValidationFailed,
    CodeAlreadyExists,
}

public sealed record CreatePlatformPlanResult(
    CreatePlatformPlanStatus Status,
    Guid? PlanId = null,
    IReadOnlyList<string>? Errors = null);

public enum UpdatePlatformPlanStatus
{
    Success,
    NotFound,
    ValidationFailed,
}

public sealed record UpdatePlatformPlanResult(
    UpdatePlatformPlanStatus Status,
    IReadOnlyList<string>? Errors = null);

public enum SetPlatformPlanActiveStatus
{
    Success,
    NotFound,
}

public sealed record SetPlatformPlanActiveResult(SetPlatformPlanActiveStatus Status);

public interface IPlatformPlanService
{
    Task<IReadOnlyList<PlatformPlanSummaryDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlatformPlanSummaryDto>> ListActiveAsync(CancellationToken cancellationToken = default);

    Task<CreatePlatformPlanResult> CreateAsync(
        CreatePlatformPlanInput input,
        CancellationToken cancellationToken = default);

    Task<UpdatePlatformPlanResult> UpdateAsync(
        Guid planId,
        UpdatePlatformPlanInput input,
        CancellationToken cancellationToken = default);

    Task<SetPlatformPlanActiveResult> SetActiveAsync(
        Guid planId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
