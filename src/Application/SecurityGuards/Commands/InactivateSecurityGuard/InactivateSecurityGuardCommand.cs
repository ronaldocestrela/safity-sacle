using MediatR;
using SafetyScale.Application.Abstractions.Persistence;

namespace SafetyScale.Application.SecurityGuards.Commands.InactivateSecurityGuard;

public sealed record InactivateSecurityGuardCommand(Guid Id) : IRequest<bool>;

public sealed class InactivateSecurityGuardCommandHandler(
    ISecurityGuardRepository securityGuardRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<InactivateSecurityGuardCommand, bool>
{
    public async Task<bool> Handle(InactivateSecurityGuardCommand request, CancellationToken cancellationToken)
    {
        var securityGuard = await securityGuardRepository.GetByIdAsync(request.Id, cancellationToken);
        if (securityGuard is null)
        {
            return false;
        }

        if (!securityGuard.IsActive)
        {
            return true;
        }

        securityGuard.IsActive = false;
        securityGuardRepository.Update(securityGuard);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
