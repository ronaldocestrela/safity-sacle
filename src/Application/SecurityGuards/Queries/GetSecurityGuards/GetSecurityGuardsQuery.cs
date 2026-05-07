using MediatR;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Application.SecurityGuards.Common;

namespace SafetyScale.Application.SecurityGuards.Queries.GetSecurityGuards;

public sealed record GetSecurityGuardsQuery(bool? IsActive) : IRequest<IReadOnlyList<SecurityGuardDto>>;

public sealed class GetSecurityGuardsQueryHandler(ISecurityGuardRepository securityGuardRepository)
    : IRequestHandler<GetSecurityGuardsQuery, IReadOnlyList<SecurityGuardDto>>
{
    public async Task<IReadOnlyList<SecurityGuardDto>> Handle(GetSecurityGuardsQuery request, CancellationToken cancellationToken)
    {
        var guards = await securityGuardRepository.GetAllAsync(cancellationToken);

        return guards
            .Where(x => !request.IsActive.HasValue || x.IsActive == request.IsActive.Value)
            .Select(x => x.ToDto())
            .ToList();
    }
}
