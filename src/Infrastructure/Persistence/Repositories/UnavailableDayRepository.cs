using Microsoft.EntityFrameworkCore;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Infrastructure.Persistence.Repositories;

public class UnavailableDayRepository(ApplicationDbContext dbContext) : IUnavailableDayRepository
{
    public async Task AddAsync(UnavailableDay unavailableDay, CancellationToken cancellationToken = default)
        => await dbContext.UnavailableDays.AddAsync(unavailableDay, cancellationToken);

    public async Task<UnavailableDay?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.UnavailableDays.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<UnavailableDay>> GetByGuardIdAsync(
        Guid securityGuardId,
        CancellationToken cancellationToken = default)
        => await dbContext.UnavailableDays
            .Where(x => x.SecurityGuardId == securityGuardId)
            .AsNoTracking()
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UnavailableDay>> GetByDateRangeAsync(
        DateOnly startInclusive,
        DateOnly endInclusive,
        CancellationToken cancellationToken = default)
        => await dbContext.UnavailableDays
            .AsNoTracking()
            .Where(x => x.Date >= startInclusive && x.Date <= endInclusive)
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsForGuardAndDateAsync(
        Guid securityGuardId,
        DateOnly date,
        CancellationToken cancellationToken = default)
        => dbContext.UnavailableDays.AnyAsync(
            x => x.SecurityGuardId == securityGuardId && x.Date == date,
            cancellationToken);

    public void Remove(UnavailableDay unavailableDay) => dbContext.UnavailableDays.Remove(unavailableDay);
}
