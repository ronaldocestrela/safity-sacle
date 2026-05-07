using FluentValidation;

namespace SafetyScale.Application.SecurityGuards.Commands.ActivateSecurityGuard;

public sealed class ActivateSecurityGuardCommandValidator : AbstractValidator<ActivateSecurityGuardCommand>
{
    public ActivateSecurityGuardCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
