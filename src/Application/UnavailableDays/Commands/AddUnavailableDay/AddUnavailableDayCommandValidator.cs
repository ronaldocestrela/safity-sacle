using FluentValidation;

namespace SafetyScale.Application.UnavailableDays.Commands.AddUnavailableDay;

public sealed class AddUnavailableDayCommandValidator : AbstractValidator<AddUnavailableDayCommand>
{
    public AddUnavailableDayCommandValidator()
    {
        RuleFor(x => x.SecurityGuardId)
            .NotEmpty();

        RuleFor(x => x.Date)
            .NotEqual(default(DateOnly));

        RuleFor(x => x.Reason!)
            .MaximumLength(250)
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
