using MediatR;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.Schedules.Common;

namespace SafetyScale.Application.Schedules.Queries.GetMonthlySchedules;

public sealed record GetMonthlySchedulesQuery(int Month, int Year) : IRequest<MonthlyScheduleDto?>;

public sealed class GetMonthlySchedulesQueryHandler(IMonthlyScheduleRepository monthlyScheduleRepository)
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

        return schedule?.ToMonthlyScheduleDto();
    }
}
