using MediatR;
using SafetyScale.Application.Abstractions.Authentication;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.Common;
using SafetyScale.Application.Schedules.Common;

namespace SafetyScale.Application.Schedules.Queries.GetMonthlySchedule;

public sealed record GetMonthlyScheduleQuery(Guid Id) : IRequest<MonthlyScheduleDto?>;

public sealed class GetMonthlyScheduleQueryHandler(
    IMonthlyScheduleRepository monthlyScheduleRepository,
    ICurrentUserContext currentUser)
    : IRequestHandler<GetMonthlyScheduleQuery, MonthlyScheduleDto?>
{
    public async Task<MonthlyScheduleDto?> Handle(
        GetMonthlyScheduleQuery request,
        CancellationToken cancellationToken)
    {
        var schedule = await monthlyScheduleRepository.GetByIdAsync(request.Id, cancellationToken);
        if (schedule is null)
        {
            return null;
        }

        var dto = schedule.ToMonthlyScheduleDto();
        if (!CurrentUserScope.IsSecurityGuardOperator(currentUser) || currentUser.SecurityGuardId is null)
        {
            return dto;
        }

        var ownItems = dto.Items
            .Where(i => i.SecurityGuardId == currentUser.SecurityGuardId.Value)
            .ToList();

        return dto with { Items = ownItems };
    }
}
