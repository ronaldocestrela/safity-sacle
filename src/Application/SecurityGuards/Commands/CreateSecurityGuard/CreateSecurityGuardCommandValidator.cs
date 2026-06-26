using FluentValidation;
using SafetyScale.Application.Abstractions.Authentication;

namespace SafetyScale.Application.SecurityGuards.Commands.CreateSecurityGuard;

public sealed class CreateSecurityGuardCommandValidator : AbstractValidator<CreateSecurityGuardCommand>
{
    public CreateSecurityGuardCommandValidator(ISecurityGuardInviteService securityGuardInviteService)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320)
            .MustAsync(async (email, cancellation) =>
                await securityGuardInviteService.IsEmailAvailableAsync(email, cancellation))
            .WithMessage("Este e-mail já está em uso.");
    }
}
