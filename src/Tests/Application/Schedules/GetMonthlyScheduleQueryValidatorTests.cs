using FluentAssertions;
using SafetyScale.Application.Schedules.Queries.GetMonthlySchedule;

namespace SafetyScale.Tests.Application.Schedules;

public class GetMonthlyScheduleQueryValidatorTests
{
    private readonly GetMonthlyScheduleQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenIdEmpty()
    {
        var result = _validator.Validate(new GetMonthlyScheduleQuery(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldPass_WhenIdNotEmpty()
    {
        var result = _validator.Validate(new GetMonthlyScheduleQuery(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }
}
