using MediatR;
using SafetyScale.Application.Abstractions.Persistence;

namespace SafetyScale.Application.SecurityGuards.Commands.UpdateSecurityGuard;

public sealed record UpdateSecurityGuardCommand(Guid Id, string Name) : IRequest<bool>;

public sealed class UpdateSecurityGuardCommandHandler(
    ISecurityGuardRepository securityGuardRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateSecurityGuardCommand, bool>
{
    public async Task<bool> Handle(UpdateSecurityGuardCommand request, CancellationToken cancellationToken)
    {
        var securityGuard = await securityGuardRepository.GetByIdAsync(request.Id, cancellationToken);
        if (securityGuard is null)
        {
            return false;
        }

        securityGuard.Name = request.Name.Trim();
        securityGuardRepository.Update(securityGuard);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
