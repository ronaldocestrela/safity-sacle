using MediatR;
using SafetyScale.Application.Abstractions.Authentication;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.Common;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Application.UnavailableDays.Commands.AddUnavailableDay;

public enum AddUnavailableDayStatus
{
    Success,
    GuardNotFound,
    GuardInactive,
    DuplicateDate,
    Forbidden,
}

public sealed record AddUnavailableDayResult(AddUnavailableDayStatus Status, Guid? Id = null);

public sealed record AddUnavailableDayCommand(Guid SecurityGuardId, DateOnly Date, string? Reason)
    : IRequest<AddUnavailableDayResult>;

public sealed class AddUnavailableDayCommandHandler(
    ISecurityGuardRepository securityGuardRepository,
    IUnavailableDayRepository unavailableDayRepository,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<AddUnavailableDayCommand, AddUnavailableDayResult>
{
    public async Task<AddUnavailableDayResult> Handle(AddUnavailableDayCommand request, CancellationToken cancellationToken)
    {
        if (!CurrentUserScope.CanAccessSecurityGuard(currentUser, request.SecurityGuardId))
        {
            return new AddUnavailableDayResult(AddUnavailableDayStatus.Forbidden);
        }

        var guard = await securityGuardRepository.GetByIdAsync(request.SecurityGuardId, cancellationToken);
        if (guard is null)
        {
            return new AddUnavailableDayResult(AddUnavailableDayStatus.GuardNotFound);
        }

        if (!guard.IsActive)
        {
            return new AddUnavailableDayResult(AddUnavailableDayStatus.GuardInactive);
        }

        if (await unavailableDayRepository.ExistsForGuardAndDateAsync(
                request.SecurityGuardId,
                request.Date,
                cancellationToken))
        {
            return new AddUnavailableDayResult(AddUnavailableDayStatus.DuplicateDate);
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

        var unavailableDay = new UnavailableDay
        {
            Id = Guid.NewGuid(),
            SecurityGuardId = request.SecurityGuardId,
            Date = request.Date,
            Reason = reason,
        };

        await unavailableDayRepository.AddAsync(unavailableDay, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AddUnavailableDayResult(AddUnavailableDayStatus.Success, unavailableDay.Id);
    }
}
