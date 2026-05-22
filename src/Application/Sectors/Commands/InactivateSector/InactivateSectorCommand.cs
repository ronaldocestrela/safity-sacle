using MediatR;
using SafetyScale.Application.Abstractions.Persistence;

namespace SafetyScale.Application.Sectors.Commands.InactivateSector;

public sealed record InactivateSectorCommand(Guid Id) : IRequest<bool>;

public sealed class InactivateSectorCommandHandler(
    ISectorRepository sectorRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<InactivateSectorCommand, bool>
{
    public async Task<bool> Handle(InactivateSectorCommand request, CancellationToken cancellationToken)
    {
        var sector = await sectorRepository.GetByIdAsync(request.Id, cancellationToken);
        if (sector is null)
        {
            return false;
        }

        if (!sector.IsActive)
        {
            return true;
        }

        sector.IsActive = false;
        sectorRepository.Update(sector);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
