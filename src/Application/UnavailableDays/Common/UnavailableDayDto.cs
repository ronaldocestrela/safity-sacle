using SafetyScale.Domain.Entities;

namespace SafetyScale.Application.UnavailableDays.Common;

public sealed record UnavailableDayDto(Guid Id, Guid SecurityGuardId, DateOnly Date, string? Reason);

public static class UnavailableDayMappings
{
    public static UnavailableDayDto ToDto(this UnavailableDay unavailableDay)
        => new(
            unavailableDay.Id,
            unavailableDay.SecurityGuardId,
            unavailableDay.Date,
            unavailableDay.Reason);
}
