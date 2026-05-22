using MediatR;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Application.Sectors.Commands.CreateSector;

public sealed record CreateSectorCommand(string Name, string? Description, int RequiredGuardsPerDay)
    : IRequest<Guid>;

public sealed class CreateSectorCommandHandler(
    ISectorRepository sectorRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateSectorCommand, Guid>
{
    public async Task<Guid> Handle(CreateSectorCommand request, CancellationToken cancellationToken)
    {
        var sector = new Sector
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            RequiredGuardsPerDay = request.RequiredGuardsPerDay,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        await sectorRepository.AddAsync(sector, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return sector.Id;
    }
}
