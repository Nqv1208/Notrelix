using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Public.Reports;

namespace Notrelix.Application.Features.Analytics.Reports.Queries.GetBoardReport;

/// <summary>
/// The work report of one Board. Read-only and open to anyone who may read the Board (ViewBoard); the
/// measures are aggregated by Work Management, so no Item is loaded here. A Board the caller cannot see
/// is NotFound (GOV-029).
/// </summary>
public record GetBoardReportQuery(Guid BoardId)
    : IQuery<Result<BoardReportDto>>, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IReadRequest
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class GetBoardReportQueryHandler : IRequestHandler<GetBoardReportQuery, Result<BoardReportDto>>
{
    private readonly IWorkReportFacts _facts;
    private readonly IResourceVisibility _visibility;
    private readonly IActorLookupService _actors;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public GetBoardReportQueryHandler(
        IWorkReportFacts facts,
        IResourceVisibility visibility,
        IActorLookupService actors,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock)
    {
        _facts = facts;
        _visibility = visibility;
        _actors = actors;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result<BoardReportDto>> Handle(GetBoardReportQuery request, CancellationToken ct)
    {
        var workspaceId = _requestContext.RequireWorkspaceId();
        var readable = await _visibility.VisibleBoardIdsAsync(workspaceId, _requestContext.UserId, ct);
        if (!readable.Contains(request.BoardId))
            throw new NotFoundException("Board", request.BoardId);

        var now = _clock.UtcNow;
        var fact = await _facts.ReportAsync(workspaceId, [request.BoardId], now, WorkReportMapper.Weeks, ct);
        var report = await WorkReportMapper.MapAsync(fact, _actors, ct);
        return Result<BoardReportDto>.Success(new BoardReportDto(request.BoardId, now, report));
    }
}
