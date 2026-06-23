namespace SafetyScale.Web.Blazor.Models.Schedules;

/// <summary>
/// Returned when automatic schedule generation cannot cover all sector positions on a given day.
/// Parity with <c>SafetyScale.Api.Contracts.Schedules.ScheduleCoverageFailureResponse</c>.
/// </summary>
public sealed class ScheduleCoverageFailureResponse
{
    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    /// <summary>First calendar day without full coverage.</summary>
    public DateOnly? FailedDate { get; init; }
}
