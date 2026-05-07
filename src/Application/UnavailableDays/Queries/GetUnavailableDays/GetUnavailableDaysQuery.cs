using MediatR;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.UnavailableDays.Common;

namespace SafetyScale.Application.UnavailableDays.Queries.GetUnavailableDays;

public sealed record GetUnavailableDaysQuery(Guid SecurityGuardId) : IRequest<GetUnavailableDaysResult>;

public sealed record GetUnavailableDaysResult(bool GuardExists, IReadOnlyList<UnavailableDayDto> Items);

public sealed class GetUnavailableDaysQueryHandler(
    ISecurityGuardRepository securityGuardRepository,
    IUnavailableDayRepository unavailableDayRepository)
    : IRequestHandler<GetUnavailableDaysQuery, GetUnavailableDaysResult>
{
    public async Task<GetUnavailableDaysResult> Handle(GetUnavailableDaysQuery request, CancellationToken cancellationToken)
    {
        var guard = await securityGuardRepository.GetByIdAsync(request.SecurityGuardId, cancellationToken);
        if (guard is null)
        {
            return new GetUnavailableDaysResult(false, Array.Empty<UnavailableDayDto>());
        }

        var days = await unavailableDayRepository.GetByGuardIdAsync(request.SecurityGuardId, cancellationToken);
        var dtoList = days.Select(x => x.ToDto()).ToList();
        return new GetUnavailableDaysResult(true, dtoList);
    }
}
