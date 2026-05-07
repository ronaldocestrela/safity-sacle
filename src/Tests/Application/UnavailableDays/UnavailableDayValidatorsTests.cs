using FluentAssertions;
using SafetyScale.Application.UnavailableDays.Commands.AddUnavailableDay;
using SafetyScale.Application.UnavailableDays.Commands.RemoveUnavailableDay;
using SafetyScale.Application.UnavailableDays.Queries.GetUnavailableDays;

namespace SafetyScale.Tests.Application.UnavailableDays;

public class UnavailableDayValidatorsTests
{
    [Fact]
    public void AddValidator_ShouldFail_WhenGuardIdEmpty()
    {
        var validator = new AddUnavailableDayCommandValidator();

        var result = validator.Validate(
            new AddUnavailableDayCommand(Guid.Empty, new DateOnly(2030, 1, 1), null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AddValidator_ShouldFail_WhenDateIsDefault()
    {
        var validator = new AddUnavailableDayCommandValidator();

        var result = validator.Validate(
            new AddUnavailableDayCommand(Guid.NewGuid(), default, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AddValidator_ShouldFail_WhenReasonTooLong()
    {
        var validator = new AddUnavailableDayCommandValidator();

        var result = validator.Validate(
            new AddUnavailableDayCommand(Guid.NewGuid(), new DateOnly(2030, 1, 1), new string('x', 251)));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RemoveValidator_ShouldFail_WhenIdEmpty()
    {
        var validator = new RemoveUnavailableDayCommandValidator();

        var result = validator.Validate(new RemoveUnavailableDayCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetValidator_ShouldFail_WhenGuardIdEmpty()
    {
        var validator = new GetUnavailableDaysQueryValidator();

        var result = validator.Validate(new GetUnavailableDaysQuery(Guid.Empty));

        result.IsValid.Should().BeFalse();
    }
}
