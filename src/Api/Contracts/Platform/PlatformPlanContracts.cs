namespace SafetyScale.Api.Contracts.Platform;

public sealed record CreatePlatformPlanRequest(
    string Name,
    string Code,
    string? Description,
    decimal PriceMonthly);

public sealed record UpdatePlatformPlanRequest(
    string Name,
    string? Description,
    decimal PriceMonthly);

public sealed record PlatformPlanResponse(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    decimal PriceMonthly,
    bool IsActive,
    DateTime CreatedAt);
