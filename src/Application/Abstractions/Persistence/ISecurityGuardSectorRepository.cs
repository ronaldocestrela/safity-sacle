namespace SafetyScale.Application.Abstractions.Persistence;

public interface ISecurityGuardSectorRepository
{
    Task ReplaceAssignmentsForGuardAsync(
        Guid securityGuardId,
        IReadOnlyList<Guid> sectorIds,
        CancellationToken cancellationToken = default);

    Task EnsureGuardLinkedToSectorAsync(
        Guid securityGuardId,
        Guid sectorId,
        CancellationToken cancellationToken = default);
}
