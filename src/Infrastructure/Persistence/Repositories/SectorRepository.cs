using Microsoft.EntityFrameworkCore;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Infrastructure.Persistence.Repositories;

public sealed class SectorRepository(ApplicationDbContext dbContext) : ISectorRepository
{
    public async Task AddAsync(Sector sector, CancellationToken cancellationToken = default)
        => await dbContext.Sectors.AddAsync(sector, cancellationToken);

    public async Task<Sector?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Sectors.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Sector>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.Sectors
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => dbContext.Sectors.CountAsync(cancellationToken);

    public void Update(Sector sector) => dbContext.Sectors.Update(sector);

    public async Task<Guid?> GetDefaultSchedulingSectorIdAsync(CancellationToken cancellationToken = default)
    {
        var id = await dbContext.Sectors
            .AsNoTracking()
            .Where(s => s.IsActive && s.RequiredGuardsPerDay >= 1)
            .OrderByDescending(s => s.Name == "Primary")
            .ThenBy(s => s.Name)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return id;
    }

    public async Task<IReadOnlyList<Sector>> GetActiveWorkloadSectorsWithLinksAsync(
        CancellationToken cancellationToken = default)
        => await dbContext.Sectors
            .AsNoTracking()
            .Include(s => s.SecurityGuardSectors)
            .ThenInclude(l => l.SecurityGuard)
            .Where(s => s.IsActive && s.RequiredGuardsPerDay >= 1)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task<bool> AllExistAndActiveAsync(
        IReadOnlyList<Guid> sectorIds,
        CancellationToken cancellationToken = default)
    {
        var distinct = sectorIds.Distinct().ToList();
        if (distinct.Count == 0)
        {
            return true;
        }

        var count = await dbContext.Sectors
            .AsNoTracking()
            .Where(x => distinct.Contains(x.Id) && x.IsActive)
            .CountAsync(cancellationToken);

        return count == distinct.Count;
    }
}
