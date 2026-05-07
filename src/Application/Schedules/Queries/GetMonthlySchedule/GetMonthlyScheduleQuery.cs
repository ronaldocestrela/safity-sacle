using MediatR;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.Schedules.Common;

namespace SafetyScale.Application.Schedules.Queries.GetMonthlySchedule;

public sealed record GetMonthlyScheduleQuery(Guid Id) : IRequest<MonthlyScheduleDto?>;

public sealed class GetMonthlyScheduleQueryHandler(IMonthlyScheduleRepository monthlyScheduleRepository)
    : IRequestHandler<GetMonthlyScheduleQuery, MonthlyScheduleDto?>
{
    public async Task<MonthlyScheduleDto?> Handle(
        GetMonthlyScheduleQuery request,
        CancellationToken cancellationToken)
    {
        var schedule = await monthlyScheduleRepository.GetByIdAsync(request.Id, cancellationToken);
        return schedule?.ToMonthlyScheduleDto();
    }
}
