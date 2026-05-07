using FluentValidation;

namespace SafetyScale.Application.SecurityGuards.Commands.UpdateSecurityGuard;

public sealed class UpdateSecurityGuardCommandValidator : AbstractValidator<UpdateSecurityGuardCommand>
{
    public UpdateSecurityGuardCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);
    }
}
