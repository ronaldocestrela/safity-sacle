namespace SafetyScale.Web.Blazor.Models.Platform;

public sealed record PlatformPlanDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    decimal PriceMonthly,
    int MaxSecurityGuards,
    int MaxSectors,
    bool IsActive,
    DateTime CreatedAt);

public sealed record CreatePlatformPlanRequestDto(
    string Name,
    string Code,
    string? Description,
    decimal PriceMonthly,
    int MaxSecurityGuards,
    int MaxSectors);

public sealed record UpdatePlatformPlanRequestDto(
    string Name,
    string? Description,
    decimal PriceMonthly,
    int MaxSecurityGuards,
    int MaxSectors);
