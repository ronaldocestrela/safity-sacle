using FluentValidation;

namespace SafetyScale.Application.UnavailableDays.Queries.GetUnavailableDays;

public sealed class GetUnavailableDaysQueryValidator : AbstractValidator<GetUnavailableDaysQuery>
{
    public GetUnavailableDaysQueryValidator()
    {
        RuleFor(x => x.SecurityGuardId)
            .NotEmpty();
    }
}
