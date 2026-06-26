using FluentValidation;
using SafetyScale.Application.Abstractions.Authentication;
using SafetyScale.Application.Abstractions.Tenancy;

namespace SafetyScale.Application.SecurityGuards.Commands.CreateSecurityGuard;

public sealed class CreateSecurityGuardCommandValidator : AbstractValidator<CreateSecurityGuardCommand>
{
    public CreateSecurityGuardCommandValidator(
        ISecurityGuardInviteService securityGuardInviteService,
        IPlanLimitEvaluator planLimitEvaluator)
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

        RuleFor(x => x)
            .CustomAsync(async (_, context, cancellation) =>
            {
                var result = await planLimitEvaluator.EvaluateCreateSecurityGuardAsync(cancellation);
                if (!result.IsAllowed)
                {
                    context.AddFailure(result.ErrorMessage ?? "Limite de seguranças do plano atingido.");
                }
            });
    }
}
