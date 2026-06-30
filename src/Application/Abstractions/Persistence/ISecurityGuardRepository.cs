using SafetyScale.Domain.Entities;

namespace SafetyScale.Application.Abstractions.Persistence;

public interface ISecurityGuardRepository
{
    Task AddAsync(SecurityGuard securityGuard, CancellationToken cancellationToken = default);
    Task<SecurityGuard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityGuard>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityGuard>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    void Update(SecurityGuard securityGuard);
}
