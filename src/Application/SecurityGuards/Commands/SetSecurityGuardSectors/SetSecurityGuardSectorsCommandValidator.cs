using FluentValidation;

namespace SafetyScale.Application.SecurityGuards.Commands.SetSecurityGuardSectors;

public sealed class SetSecurityGuardSectorsCommandValidator : AbstractValidator<SetSecurityGuardSectorsCommand>
{
    public SetSecurityGuardSectorsCommandValidator()
    {
        RuleFor(x => x.GuardId)
            .NotEmpty();

        RuleForEach(x => x.SectorIds)
            .NotEmpty();
    }
}
