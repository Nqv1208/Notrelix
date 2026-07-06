using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Common.Security;

public class WorkspacePermissionService : IWorkspacePermissionService
{
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly IWorkManagementDbContext _context;

    public WorkspacePermissionService(IPermissionEvaluator permissionEvaluator, IWorkManagementDbContext context)
    {
        _permissionEvaluator = permissionEvaluator;
        _context = context;
    }

    public async Task<bool> CanViewWorkspaceAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty || userId == Guid.Empty) return false;

        var decision = await _permissionEvaluator.EvaluateAsync(
            new PermissionContext(userId, Guid.Empty, workspaceId, ResourceType.Workspace, null, PermissionAction.ViewWorkspace, PermissionScope.Workspace),
            cancellationToken);

        return decision.IsAllowed;
    }

    public async Task<bool> CanEditWorkspaceAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty || userId == Guid.Empty) return false;

        var decision = await _permissionEvaluator.EvaluateAsync(
            new PermissionContext(userId, Guid.Empty, workspaceId, ResourceType.Workspace, null, PermissionAction.ManageWorkspace, PermissionScope.Workspace),
            cancellationToken);

        return decision.IsAllowed;
    }

    public async Task<bool> CanManageWorkspaceAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty || userId == Guid.Empty) return false;

        var decision = await _permissionEvaluator.EvaluateAsync(
            new PermissionContext(userId, Guid.Empty, workspaceId, ResourceType.Workspace, null, PermissionAction.DeleteWorkspace, PermissionScope.Workspace),
            cancellationToken);

        return decision.IsAllowed;
    }

    public async Task<bool> CanEditBoardAsync(Guid boardId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (boardId == Guid.Empty || userId == Guid.Empty) return false;

        var workspaceId = await ResolveBoardWorkspaceAsync(boardId, cancellationToken);
        if (workspaceId is null) return false;

        var decision = await _permissionEvaluator.EvaluateAsync(
            new PermissionContext(userId, Guid.Empty, workspaceId.Value, ResourceType.Board, boardId, PermissionAction.UpdateItem, PermissionScope.Resource),
            cancellationToken);

        return decision.IsAllowed;
    }

    public async Task<bool> CanManageBoardAsync(Guid boardId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (boardId == Guid.Empty || userId == Guid.Empty) return false;

        var workspaceId = await ResolveBoardWorkspaceAsync(boardId, cancellationToken);
        if (workspaceId is null) return false;

        var decision = await _permissionEvaluator.EvaluateAsync(
            new PermissionContext(userId, Guid.Empty, workspaceId.Value, ResourceType.Board, boardId, PermissionAction.ManageBoardPermission, PermissionScope.Resource),
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

    private async Task<Guid?> ResolveBoardWorkspaceAsync(Guid boardId, CancellationToken cancellationToken)
    {
        return await _context.Boards
            .AsNoTracking()
            .Where(b => b.Id == boardId && !b.IsArchived)
            .Select(b => (Guid?)b.WorkspaceId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
