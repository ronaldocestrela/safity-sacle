using FluentAssertions;
using SafetyScale.Application.Schedules.Commands.GenerateMonthlySchedule;

namespace SafetyScale.Tests.Application.Schedules;

public class GenerateMonthlyScheduleCommandValidatorTests
{
    private readonly GenerateMonthlyScheduleCommandValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Validate_ShouldFail_WhenMonthOutOfRange(int month)
    {
        var result = _validator.Validate(new GenerateMonthlyScheduleCommand(month, 2030));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void Validate_ShouldFail_WhenYearOutOfRange(int year)
    {
        var result = _validator.Validate(new GenerateMonthlyScheduleCommand(6, year));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldPass_ForValidMonthYear()
    {
        var result = _validator.Validate(new GenerateMonthlyScheduleCommand(12, 2050));
        result.IsValid.Should().BeTrue();
    }
}
