using MediatR;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.Sectors.Common;

namespace SafetyScale.Application.Sectors.Queries.GetSectors;

public sealed record GetSectorsQuery(bool? IsActive) : IRequest<IReadOnlyList<SectorDto>>;

public sealed class GetSectorsQueryHandler(ISectorRepository sectorRepository)
    : IRequestHandler<GetSectorsQuery, IReadOnlyList<SectorDto>>
{
    public async Task<IReadOnlyList<SectorDto>> Handle(GetSectorsQuery request, CancellationToken cancellationToken)
    {
        var sectors = await sectorRepository.GetAllAsync(cancellationToken);

        return sectors
            .Where(s => !request.IsActive.HasValue || s.IsActive == request.IsActive.Value)
            .Select(x => x.ToDto())
            .ToList();
    }
}
