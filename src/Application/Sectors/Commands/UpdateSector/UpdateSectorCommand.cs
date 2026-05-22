using MediatR;
using SafetyScale.Application.Abstractions.Persistence;

namespace SafetyScale.Application.Sectors.Commands.UpdateSector;

public sealed record UpdateSectorCommand(Guid Id, string Name, string? Description) : IRequest<bool>;

public sealed class UpdateSectorCommandHandler(
    ISectorRepository sectorRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateSectorCommand, bool>
{
    public async Task<bool> Handle(UpdateSectorCommand request, CancellationToken cancellationToken)
    {
        var sector = await sectorRepository.GetByIdAsync(request.Id, cancellationToken);
        if (sector is null)
        {
            return false;
        }

        sector.Name = request.Name.Trim();
        sector.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        sectorRepository.Update(sector);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
