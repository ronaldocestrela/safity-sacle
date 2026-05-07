using Microsoft.EntityFrameworkCore;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Infrastructure.Persistence.Repositories;

public class SecurityGuardRepository(ApplicationDbContext dbContext) : ISecurityGuardRepository
{
    public async Task AddAsync(SecurityGuard securityGuard, CancellationToken cancellationToken = default)
        => await dbContext.SecurityGuards.AddAsync(securityGuard, cancellationToken);

    public async Task<SecurityGuard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.SecurityGuards.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SecurityGuard>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.SecurityGuards.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SecurityGuard>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await dbContext.SecurityGuards
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public void Update(SecurityGuard securityGuard) => dbContext.SecurityGuards.Update(securityGuard);
}
