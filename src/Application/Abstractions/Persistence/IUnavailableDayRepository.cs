using SafetyScale.Domain.Entities;

namespace SafetyScale.Application.Abstractions.Persistence;

public interface IUnavailableDayRepository
{
    Task AddAsync(UnavailableDay unavailableDay, CancellationToken cancellationToken = default);
    Task<UnavailableDay?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnavailableDay>> GetByGuardIdAsync(Guid securityGuardId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForGuardAndDateAsync(
        Guid securityGuardId,
        DateOnly date,
        CancellationToken cancellationToken = default);
    void Remove(UnavailableDay unavailableDay);
}
