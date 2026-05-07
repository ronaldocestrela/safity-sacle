using FluentValidation;

namespace SafetyScale.Application.Schedules.Queries.GetMonthlySchedules;

public sealed class GetMonthlySchedulesQueryValidator : AbstractValidator<GetMonthlySchedulesQuery>
{
    public GetMonthlySchedulesQueryValidator()
    {
        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12);

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100);
    }
}
