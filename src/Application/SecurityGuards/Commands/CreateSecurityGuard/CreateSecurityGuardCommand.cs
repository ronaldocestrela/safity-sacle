using MediatR;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Application.SecurityGuards.Commands.CreateSecurityGuard;

public sealed record CreateSecurityGuardCommand(string Name) : IRequest<Guid>;

public sealed class CreateSecurityGuardCommandHandler(
    ISecurityGuardRepository securityGuardRepository,
    ISectorRepository sectorRepository,
    ISecurityGuardSectorRepository securityGuardSectorRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateSecurityGuardCommand, Guid>
{
    public async Task<Guid> Handle(CreateSecurityGuardCommand request, CancellationToken cancellationToken)
    {
        var securityGuard = new SecurityGuard
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await securityGuardRepository.AddAsync(securityGuard, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var defaultSectorId = await sectorRepository.GetDefaultSchedulingSectorIdAsync(cancellationToken);
        if (defaultSectorId.HasValue)
        {
            await securityGuardSectorRepository.EnsureGuardLinkedToSectorAsync(
                securityGuard.Id,
                defaultSectorId.Value,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return securityGuard.Id;
    }
}
