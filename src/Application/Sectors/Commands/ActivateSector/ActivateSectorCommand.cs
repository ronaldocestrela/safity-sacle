using MediatR;
using SafetyScale.Application.Abstractions.Persistence;

namespace SafetyScale.Application.Sectors.Commands.ActivateSector;

public sealed record ActivateSectorCommand(Guid Id) : IRequest<bool>;

public sealed class ActivateSectorCommandHandler(
    ISectorRepository sectorRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ActivateSectorCommand, bool>
{
    public async Task<bool> Handle(ActivateSectorCommand request, CancellationToken cancellationToken)
    {
        var sector = await sectorRepository.GetByIdAsync(request.Id, cancellationToken);
        if (sector is null)
        {
            return false;
        }

        if (sector.IsActive)
        {
            return true;
        }

        sector.IsActive = true;
        sectorRepository.Update(sector);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
