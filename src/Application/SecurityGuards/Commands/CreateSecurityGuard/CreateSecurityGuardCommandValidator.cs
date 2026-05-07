using FluentValidation;

namespace SafetyScale.Application.SecurityGuards.Commands.CreateSecurityGuard;

public sealed class CreateSecurityGuardCommandValidator : AbstractValidator<CreateSecurityGuardCommand>
{
    public CreateSecurityGuardCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);
    }
}
