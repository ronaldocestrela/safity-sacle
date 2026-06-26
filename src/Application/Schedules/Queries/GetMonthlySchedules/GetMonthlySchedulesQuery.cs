using MediatR;
using SafetyScale.Application.Abstractions.Authentication;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.Common;
using SafetyScale.Application.Schedules.Common;

namespace SafetyScale.Application.Schedules.Queries.GetMonthlySchedules;

public sealed record GetMonthlySchedulesQuery(int Month, int Year) : IRequest<MonthlyScheduleDto?>;

public sealed class GetMonthlySchedulesQueryHandler(
    IMonthlyScheduleRepository monthlyScheduleRepository,
    ICurrentUserContext currentUser)
    : IRequestHandler<GetMonthlySchedulesQuery, MonthlyScheduleDto?>
{
    public async Task<MonthlyScheduleDto?> Handle(
        GetMonthlySchedulesQuery request,
        CancellationToken cancellationToken)
    {
        var schedule = await monthlyScheduleRepository.GetByMonthYearAsync(
            request.Month,
            request.Year,
            cancellationToken);

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
