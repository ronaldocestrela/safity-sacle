using FluentAssertions;
using SafetyScale.Application.Schedules.Queries.GetMonthlySchedules;

namespace SafetyScale.Tests.Application.Schedules;

public class GetMonthlySchedulesQueryValidatorTests
{
    private readonly GetMonthlySchedulesQueryValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Validate_ShouldFail_WhenMonthOutOfRange(int month)
    {
        var result = _validator.Validate(new GetMonthlySchedulesQuery(month, 2060));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void Validate_ShouldFail_WhenYearOutOfRange(int year)
    {
        var result = _validator.Validate(new GetMonthlySchedulesQuery(8, year));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldPass_ForValidMonthYear()
    {
        var result = _validator.Validate(new GetMonthlySchedulesQuery(7, 2055));
        result.IsValid.Should().BeTrue();
    }
}
