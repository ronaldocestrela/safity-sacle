using MediatR;
using SafetyScale.Application.Abstractions.Persistence;

namespace SafetyScale.Application.SecurityGuards.Commands.SetSecurityGuardSectors;

public enum SetSecurityGuardSectorsStatus
{
    Success,
    GuardNotFound,
    InvalidSectors,
}

public sealed record SetSecurityGuardSectorsCommand(Guid GuardId, IReadOnlyList<Guid> SectorIds)
    : IRequest<SetSecurityGuardSectorsStatus>;

public sealed class SetSecurityGuardSectorsCommandHandler(
    ISecurityGuardRepository securityGuardRepository,
    ISecurityGuardSectorRepository securityGuardSectorRepository,
    ISectorRepository sectorRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SetSecurityGuardSectorsCommand, SetSecurityGuardSectorsStatus>
{
    public async Task<SetSecurityGuardSectorsStatus> Handle(
        SetSecurityGuardSectorsCommand request,
        CancellationToken cancellationToken)
    {
        var guardExists = await securityGuardRepository.GetByIdAsync(request.GuardId, cancellationToken)
            is not null;
        if (!guardExists)
        {
            return SetSecurityGuardSectorsStatus.GuardNotFound;
        }

        var valid = await sectorRepository.AllExistAndActiveAsync(request.SectorIds, cancellationToken);
        if (!valid)
        {
            return SetSecurityGuardSectorsStatus.InvalidSectors;
        }

        await securityGuardSectorRepository.ReplaceAssignmentsForGuardAsync(
            request.GuardId,
            request.SectorIds,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return SetSecurityGuardSectorsStatus.Success;
    }
}
