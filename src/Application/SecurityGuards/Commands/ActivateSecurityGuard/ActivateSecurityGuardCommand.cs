using MediatR;
using SafetyScale.Application.Abstractions.Persistence;

namespace SafetyScale.Application.SecurityGuards.Commands.ActivateSecurityGuard;

public sealed record ActivateSecurityGuardCommand(Guid Id) : IRequest<bool>;

public sealed class ActivateSecurityGuardCommandHandler(
    ISecurityGuardRepository securityGuardRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ActivateSecurityGuardCommand, bool>
{
    public async Task<bool> Handle(ActivateSecurityGuardCommand request, CancellationToken cancellationToken)
    {
        var securityGuard = await securityGuardRepository.GetByIdAsync(request.Id, cancellationToken);
        if (securityGuard is null)
        {
            return false;
        }

        if (securityGuard.IsActive)
        {
            return true;
        }

        securityGuard.IsActive = true;
        securityGuardRepository.Update(securityGuard);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
