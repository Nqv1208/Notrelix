using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Scoping;

namespace Notrelix.Application.Features.WorkManagement.Limits;

/// <summary>The published board limits (D78.8, UC-WM-T03), the same for every board and every caller.</summary>
public sealed record GetWorkLimitsQuery
    : IQuery<Result<WorkLimitsDto>>, IReadRequest,
      IGlobalRequest,
      IAuthenticatedRequest;

public sealed class GetWorkLimitsQueryHandler : IRequestHandler<GetWorkLimitsQuery, Result<WorkLimitsDto>>
{
    public Task<Result<WorkLimitsDto>> Handle(GetWorkLimitsQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result<WorkLimitsDto>.Success(WorkLimits.Published));
}
