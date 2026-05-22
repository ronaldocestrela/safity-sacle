using Microsoft.EntityFrameworkCore;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Infrastructure.Persistence.Repositories;

public sealed class SecurityGuardSectorRepository(ApplicationDbContext dbContext) : ISecurityGuardSectorRepository
{
    public async Task ReplaceAssignmentsForGuardAsync(
        Guid securityGuardId,
        IReadOnlyList<Guid> sectorIds,
        CancellationToken cancellationToken = default)
    {
        var distinct = sectorIds.Distinct().ToList();

        var existing = await dbContext.SecurityGuardSectors
            .Where(x => x.SecurityGuardId == securityGuardId)
            .ToListAsync(cancellationToken);

        dbContext.SecurityGuardSectors.RemoveRange(existing);

        foreach (var sid in distinct)
        {
            await dbContext.SecurityGuardSectors.AddAsync(new SecurityGuardSector
            {
                Id = Guid.NewGuid(),
                SecurityGuardId = securityGuardId,
                SectorId = sid,
            }, cancellationToken);
        }
    }
}
