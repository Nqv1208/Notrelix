namespace Notrelix.Application.Common.Interfaces;

public interface IWorkspacePermissionService
{
    Task<bool> CanViewWorkspaceAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CanEditWorkspaceAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CanManageWorkspaceAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CanEditBoardAsync(Guid boardId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CanManageBoardAsync(Guid boardId, Guid userId, CancellationToken cancellationToken = default);
    Task EnsureCanManageWorkspaceAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);
    Task EnsureCanEditBoardAsync(Guid boardId, Guid userId, CancellationToken cancellationToken = default);
    Task EnsureCanManageBoardAsync(Guid boardId, Guid userId, CancellationToken cancellationToken = default);
}
