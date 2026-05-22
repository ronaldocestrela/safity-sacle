using FluentValidation;

namespace SafetyScale.Application.Sectors.Commands.CreateSector;

public sealed class CreateSectorCommandValidator : AbstractValidator<CreateSectorCommand>
{
    public CreateSectorCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);

        RuleFor(x => x.RequiredGuardsPerDay)
            .InclusiveBetween(1, 500);
    }
}
