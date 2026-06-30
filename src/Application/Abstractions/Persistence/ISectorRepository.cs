using SafetyScale.Domain.Entities;

namespace SafetyScale.Application.Abstractions.Persistence;

public interface ISectorRepository
{
    Task AddAsync(Sector sector, CancellationToken cancellationToken = default);
    Task<Sector?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sector>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    void Update(Sector sector);

    Task<Guid?> GetDefaultSchedulingSectorIdAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Sector>> GetActiveWorkloadSectorsWithLinksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// True when every distinct id references an existing, active sector in the tenant.
    /// Vacuous truth when ids is empty.
    /// </summary>
    Task<bool> AllExistAndActiveAsync(IReadOnlyList<Guid> sectorIds, CancellationToken cancellationToken = default);
}
