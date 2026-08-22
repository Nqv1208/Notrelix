namespace Notrelix.Application.Common.Security;

public class WorkspacePermissionService : IWorkspacePermissionService
{
    private static readonly ResourceKind WorkspaceKind = ResourceKind.Create("workspaces.workspace");
    private static readonly ResourceKind BoardKind = ResourceKind.Create("work-management.board");

    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly IResourceAuthorizationSnapshotStore _resourceSnapshots;

    public WorkspacePermissionService(
        IPermissionEvaluator permissionEvaluator,
        IResourceAuthorizationSnapshotStore resourceSnapshots)
    {
        _permissionEvaluator = permissionEvaluator;
        _resourceSnapshots = resourceSnapshots;
    }

    private static void GuardUserId(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new UnauthorizedException("User ID is required for permission evaluation.");
    }

    public async Task<bool> CanViewWorkspaceAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        GuardUserId(userId);
        if (workspaceId == Guid.Empty) return false;

        var decision = await _permissionEvaluator.EvaluateAsync(
            new PermissionContext(userId, Guid.Empty, workspaceId, WorkspaceKind, null, PermissionAction.ViewWorkspace, PermissionScope.Workspace),
            cancellationToken);

        return decision.IsAllowed;
    }

    public async Task<bool> CanEditWorkspaceAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        GuardUserId(userId);
        if (workspaceId == Guid.Empty) return false;

        var decision = await _permissionEvaluator.EvaluateAsync(
            new PermissionContext(userId, Guid.Empty, workspaceId, WorkspaceKind, null, PermissionAction.ManageWorkspace, PermissionScope.Workspace),
            cancellationToken);

        return decision.IsAllowed;
    }

    public async Task<bool> CanManageWorkspaceAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        GuardUserId(userId);
        if (workspaceId == Guid.Empty) return false;

        var decision = await _permissionEvaluator.EvaluateAsync(
            new PermissionContext(userId, Guid.Empty, workspaceId, WorkspaceKind, null, PermissionAction.DeleteWorkspace, PermissionScope.Workspace),
            cancellationToken);

        return decision.IsAllowed;
    }

    public async Task<bool> CanEditBoardAsync(Guid boardId, Guid userId, CancellationToken cancellationToken = default)
    {
        GuardUserId(userId);
        if (boardId == Guid.Empty) return false;

        var workspaceId = await ResolveBoardWorkspaceAsync(boardId, userId, cancellationToken);
        if (workspaceId is null) return false;

        var decision = await _permissionEvaluator.EvaluateAsync(
            new PermissionContext(userId, Guid.Empty, workspaceId.Value, BoardKind, boardId, PermissionAction.UpdateItem, PermissionScope.Resource),
            cancellationToken);

        return decision.IsAllowed;
    }

    public async Task<bool> CanManageBoardAsync(Guid boardId, Guid userId, CancellationToken cancellationToken = default)
    {
        GuardUserId(userId);
        if (boardId == Guid.Empty) return false;

        var workspaceId = await ResolveBoardWorkspaceAsync(boardId, userId, cancellationToken);
        if (workspaceId is null) return false;

        var decision = await _permissionEvaluator.EvaluateAsync(
            new PermissionContext(userId, Guid.Empty, workspaceId.Value, BoardKind, boardId, PermissionAction.ManageBoardPermission, PermissionScope.Resource),
            cancellationToken);

        return decision.IsAllowed;
    }

    public async Task EnsureCanManageWorkspaceAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await CanManageWorkspaceAsync(workspaceId, userId, cancellationToken))
        {
            throw new ForbiddenException("Bạn không có quyền quản lý workspace này.");
        }
    }

    public async Task EnsureCanEditBoardAsync(Guid boardId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await CanEditBoardAsync(boardId, userId, cancellationToken))
        {
            throw new ForbiddenException("Bạn không có quyền chỉnh sửa board này.");
        }
    }

    public async Task EnsureCanManageBoardAsync(Guid boardId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await CanManageBoardAsync(boardId, userId, cancellationToken))
        {
            throw new ForbiddenException("Bạn không có quyền quản lý board này.");
        }
    }

    private async Task<Guid?> ResolveBoardWorkspaceAsync(
        Guid boardId,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _resourceSnapshots.ResolveAsync(
            BoardKind,
            boardId,
            actorId,
            cancellationToken);

        return snapshot?.WorkspaceId;
    }
}
