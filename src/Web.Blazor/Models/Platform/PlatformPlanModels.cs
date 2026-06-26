namespace SafetyScale.Web.Blazor.Models.Platform;

public sealed record PlatformPlanDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    decimal PriceMonthly,
    bool IsActive,
    DateTime CreatedAt);

public sealed record CreatePlatformPlanRequestDto(
    string Name,
    string Code,
    string? Description,
    decimal PriceMonthly);

public sealed record UpdatePlatformPlanRequestDto(
    string Name,
    string? Description,
    decimal PriceMonthly);
