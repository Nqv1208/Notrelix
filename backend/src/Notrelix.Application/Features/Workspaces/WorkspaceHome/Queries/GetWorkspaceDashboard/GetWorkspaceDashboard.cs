using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

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
) : IQuery<Result<WorkspaceDashboardDto>>, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetWorkspaceDashboardQueryHandler : IRequestHandler<GetWorkspaceDashboardQuery, Result<WorkspaceDashboardDto>>
{
    private readonly IWorkspaceDbContext _context;

    public GetWorkspaceDashboardQueryHandler(IWorkspaceDbContext context)
    {
        _context = context;
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

        var spaceCount = await _context.Spaces
            .AsNoTracking()
            .CountAsync(s => s.WorkspaceId == request.WorkspaceId && !s.IsDeleted, ct);

        var teamCount = await _context.Teams
            .AsNoTracking()
            .CountAsync(t => t.WorkspaceId == request.WorkspaceId && !t.IsDeleted, ct);

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
