using SafetyScale.Domain.Entities;

namespace SafetyScale.Domain.Services;

public enum ScheduleGenerationFailureReason
{
    NoActiveGuards,
    NoConfiguredSectors,
    NoEligibleGuardsForSectors,
    CouldNotCoverDay,
}

public sealed record ScheduleGenerationResult(
    bool Success,
    MonthlySchedule? Schedule,
    ScheduleGenerationFailureReason? FailureReason,
    DateOnly? FailedDate)
{
    public static ScheduleGenerationResult Ok(MonthlySchedule schedule) =>
        new(true, schedule, null, null);

    public static ScheduleGenerationResult Fail(ScheduleGenerationFailureReason reason, DateOnly? failedDate) =>
        new(false, null, reason, failedDate);
}
