using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Public.Reports;
using Notrelix.Application.Features.Workspaces.Public.SpaceSubtree;

namespace Notrelix.Application.Features.Analytics.Reports.Queries.GetWorkspaceReport;

/// <summary>
/// The work report across the Boards of a Workspace the caller may read (ViewWorkspace, then the Governance
/// visible Board ids). Workspace Owners and Admins may narrow it to a department Space and everything below
/// it; that needs ManageSpaces on the Workspace, which the pipeline authorizes, so a plain member asking for
/// a Space is refused, never silently widened.
/// </summary>
public record GetWorkspaceReportQuery(Guid WorkspaceId, Guid? SpaceId = null, int Weeks = WorkReportPeriod.DefaultWeeks)
    : IQuery<Result<WorkspaceReportDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => SpaceId is null ? PermissionAction.ViewWorkspace : PermissionAction.ManageSpaces;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetWorkspaceReportQueryHandler : IRequestHandler<GetWorkspaceReportQuery, Result<WorkspaceReportDto>>
{
    private readonly IWorkReportFacts _facts;
    private readonly IResourceVisibility _visibility;
    private readonly ISpaceSubtreeFacts _spaces;
    private readonly IActorLookupService _actors;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public GetWorkspaceReportQueryHandler(
        IWorkReportFacts facts,
        IResourceVisibility visibility,
        ISpaceSubtreeFacts spaces,
        IActorLookupService actors,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock)
    {
        _facts = facts;
        _visibility = visibility;
        _spaces = spaces;
        _actors = actors;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result<WorkspaceReportDto>> Handle(GetWorkspaceReportQuery request, CancellationToken ct)
    {
        var workspaceId = request.WorkspaceId;
        var userId = _requestContext.UserId;
        IReadOnlyList<Guid> boards = await _visibility.VisibleBoardIdsAsync(workspaceId, userId, ct);

        if (request.SpaceId is { } spaceId)
        {
            var visibleSpaces = await _visibility.VisibleSpaceIdsAsync(workspaceId, userId, ct);
            var subtree = visibleSpaces.Contains(spaceId)
                ? await _spaces.GetSubtreeAsync(workspaceId, spaceId, ct)
                : null;
            if (subtree is null)
                throw new NotFoundException("Space", spaceId);

            boards = await _facts.BoardIdsInSpacesAsync(workspaceId, boards, subtree, ct);
        }

        var now = _clock.UtcNow;
        var fact = await _facts.ReportAsync(workspaceId, boards, now, request.Weeks, ct);
        var report = await WorkReportMapper.MapAsync(fact, _actors, ct);
        return Result<WorkspaceReportDto>.Success(new WorkspaceReportDto(now, boards.Count, request.SpaceId, report));
    }
}
