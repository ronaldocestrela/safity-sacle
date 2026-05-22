namespace SafetyScale.Application.Abstractions.Persistence;

public interface ISecurityGuardSectorRepository
{
    Task ReplaceAssignmentsForGuardAsync(
        Guid securityGuardId,
        IReadOnlyList<Guid> sectorIds,
        CancellationToken cancellationToken = default);
}
