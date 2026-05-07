using FluentValidation;

namespace SafetyScale.Application.UnavailableDays.Commands.RemoveUnavailableDay;

public sealed class RemoveUnavailableDayCommandValidator : AbstractValidator<RemoveUnavailableDayCommand>
{
    public RemoveUnavailableDayCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
