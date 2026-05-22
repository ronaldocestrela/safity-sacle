using FluentValidation;

namespace SafetyScale.Application.Sectors.Commands.UpdateSector;

public sealed class UpdateSectorCommandValidator : AbstractValidator<UpdateSectorCommand>
{
    public UpdateSectorCommandValidator()
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
