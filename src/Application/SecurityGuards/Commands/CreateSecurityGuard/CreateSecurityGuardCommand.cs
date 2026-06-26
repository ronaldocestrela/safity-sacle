using MediatR;
using SafetyScale.Application.Abstractions.Authentication;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Application.SecurityGuards.Commands.CreateSecurityGuard;

public sealed record CreateSecurityGuardCommand(string Name, string Email) : IRequest<Guid>;

public sealed class CreateSecurityGuardCommandHandler(
    ISecurityGuardRepository securityGuardRepository,
    ISectorRepository sectorRepository,
    ISecurityGuardSectorRepository securityGuardSectorRepository,
    ISecurityGuardInviteService securityGuardInviteService,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateSecurityGuardCommand, Guid>
{
    public async Task<Guid> Handle(CreateSecurityGuardCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var email = request.Email.Trim();

        var securityGuard = new SecurityGuard
        {
            Id = Guid.NewGuid(),
            Name = name,
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

        await securityGuardInviteService.InviteAsync(
            securityGuard.Id,
            email,
            name,
            cancellationToken);

        return securityGuard.Id;
    }
}
