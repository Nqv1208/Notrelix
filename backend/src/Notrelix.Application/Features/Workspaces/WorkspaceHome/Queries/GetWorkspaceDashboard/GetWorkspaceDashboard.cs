using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Governance.Public.Visibility;

namespace Notrelix.Application.Features.Workspaces.WorkspaceHome.Queries.GetWorkspaceDashboard;

public record WorkspaceDashboardDto(
    Guid WorkspaceId,
    string WorkspaceName,
    int MemberCount,
    int SpaceCount,
    int TeamCount,
    int InvitationCount,
    bool IsArchived
);

public record GetWorkspaceDashboardQuery(
    Guid WorkspaceId
) : IQuery<Result<WorkspaceDashboardDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetWorkspaceDashboardQueryHandler : IRequestHandler<GetWorkspaceDashboardQuery, Result<WorkspaceDashboardDto>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetWorkspaceDashboardQueryHandler(IWorkspaceDbContext context, IResourceVisibility visibility, ICurrentRequestContext requestContext)
    {
        _context = context;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<WorkspaceDashboardDto>> Handle(GetWorkspaceDashboardQuery request, CancellationToken ct)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, ct);

        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var memberCount = await _context.WorkspaceMembers
            .AsNoTracking()
            .CountAsync(m => m.WorkspaceId == request.WorkspaceId, ct);

        // Counts only the Spaces the caller can see; a Private Space's existence
        // is not disclosed through the dashboard (GOV-029).
        var spaceCount = (await _visibility.VisibleSpaceIdsAsync(request.WorkspaceId, _requestContext.UserId, ct)).Count;

        var teamCount = await _context.Teams
            .AsNoTracking()
            .CountAsync(t => t.WorkspaceId == request.WorkspaceId && t.DeletedAt == null, ct);

        var invitationCount = await _context.WorkspaceInvitations
            .AsNoTracking()
            .CountAsync(i => i.WorkspaceId == request.WorkspaceId && i.Status == WorkspaceInvitationStatus.Pending, ct);

        return Result<WorkspaceDashboardDto>.Success(new WorkspaceDashboardDto(
            workspace.Id,
            workspace.Name,
            memberCount,
            spaceCount,
            teamCount,
            invitationCount,
            workspace.Status == WorkspaceStatus.Archived));
    }
}
