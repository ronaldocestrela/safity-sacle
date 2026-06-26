using MediatR;
using SafetyScale.Application.Abstractions.Authentication;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.Common;

namespace SafetyScale.Application.UnavailableDays.Commands.RemoveUnavailableDay;

public sealed record RemoveUnavailableDayCommand(Guid Id) : IRequest<bool>;

public sealed class RemoveUnavailableDayCommandHandler(
    IUnavailableDayRepository unavailableDayRepository,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<RemoveUnavailableDayCommand, bool>
{
    public async Task<bool> Handle(RemoveUnavailableDayCommand request, CancellationToken cancellationToken)
    {
        var entity = await unavailableDayRepository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        if (!CurrentUserScope.CanAccessSecurityGuard(currentUser, entity.SecurityGuardId))
        {
            return false;
        }

        unavailableDayRepository.Remove(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
