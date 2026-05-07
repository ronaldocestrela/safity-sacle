using FluentValidation;

namespace SafetyScale.Application.SecurityGuards.Commands.InactivateSecurityGuard;

public sealed class InactivateSecurityGuardCommandValidator : AbstractValidator<InactivateSecurityGuardCommand>
{
    public InactivateSecurityGuardCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
