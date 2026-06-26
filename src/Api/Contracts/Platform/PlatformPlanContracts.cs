namespace SafetyScale.Api.Contracts.Platform;

public sealed record CreatePlatformPlanRequest(
    string Name,
    string Code,
    string? Description,
    decimal PriceMonthly,
    int MaxSecurityGuards,
    int MaxSectors);

public sealed record UpdatePlatformPlanRequest(
    string Name,
    string? Description,
    decimal PriceMonthly,
    int MaxSecurityGuards,
    int MaxSectors);

public sealed record PlatformPlanResponse(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    decimal PriceMonthly,
    int MaxSecurityGuards,
    int MaxSectors,
    bool IsActive,
    DateTime CreatedAt);
