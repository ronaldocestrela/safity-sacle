using FluentValidation;

namespace SafetyScale.Application.Schedules.Queries.GetMonthlySchedule;

public sealed class GetMonthlyScheduleQueryValidator : AbstractValidator<GetMonthlyScheduleQuery>
{
    public GetMonthlyScheduleQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
