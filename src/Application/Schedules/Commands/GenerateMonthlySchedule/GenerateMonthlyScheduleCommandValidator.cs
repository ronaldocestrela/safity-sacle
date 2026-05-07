using FluentValidation;

namespace SafetyScale.Application.Schedules.Commands.GenerateMonthlySchedule;

public sealed class GenerateMonthlyScheduleCommandValidator : AbstractValidator<GenerateMonthlyScheduleCommand>
{
    public GenerateMonthlyScheduleCommandValidator()
    {
        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12);

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100);
    }
}
