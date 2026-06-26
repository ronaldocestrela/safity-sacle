using FluentValidation;
using SafetyScale.Application.Abstractions.Tenancy;

namespace SafetyScale.Application.Sectors.Commands.CreateSector;

public sealed class CreateSectorCommandValidator : AbstractValidator<CreateSectorCommand>
{
    public CreateSectorCommandValidator(IPlanLimitEvaluator planLimitEvaluator)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);

        RuleFor(x => x.RequiredGuardsPerDay)
            .InclusiveBetween(1, 500);

        RuleFor(x => x)
            .CustomAsync(async (_, context, cancellation) =>
            {
                var result = await planLimitEvaluator.EvaluateCreateSectorAsync(cancellation);
                if (!result.IsAllowed)
                {
                    context.AddFailure(result.ErrorMessage ?? "Limite de setores do plano atingido.");
                }
            });
    }
}
