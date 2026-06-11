using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Domain.WorkManagement.Boards;

namespace Notrelix.Application.Common.Security;

public class PermissionService : IPermissionService
{
    private readonly IApplicationDbContext _context;

    public PermissionService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PermissionDecision> EvaluateAsync(
        PermissionContext context,
        CancellationToken cancellationToken = default)
    {
        // 1. Check workspace membership
        var workspaceMember = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == context.WorkspaceId && m.UserId == context.UserId, cancellationToken);

        if (workspaceMember is null)
        {
            return new PermissionDecision(false, "not_workspace_member");
        }

        // 2. Owner has all rights
        if (workspaceMember.Role == WorkspaceRole.Owner)
        {
            return new PermissionDecision(true, null, PermissionLevel.Owner);
        }

        // 3. DeleteWorkspace is typically Owner only
        if (context.Action == PermissionAction.DeleteWorkspace)
        {
            return new PermissionDecision(false, "missing_permission");
        }

        // 4. Resource specific permissions
        if (context.ResourceType == ResourceType.Board && context.ResourceId.HasValue)
        {
            var board = await _context.Boards
                .FirstOrDefaultAsync(b => b.Id == context.ResourceId.Value && b.WorkspaceId == context.WorkspaceId, cancellationToken);

            if (board is null || board.IsArchived)
            {
                return new PermissionDecision(false, "resource_not_found");
            }

            // Check if board is private
            if (board.Visibility == BoardVisibility.Private)
            {
                // Must be a board member or have explicit resource permission
                var boardMember = await _context.BoardMembers
                    .FirstOrDefaultAsync(m => m.BoardId == board.Id && m.UserId == context.UserId, cancellationToken);

                var hasExplicitPermission = await _context.ResourcePermissions
                    .AnyAsync(p => p.WorkspaceId == context.WorkspaceId &&
                                   p.ResourceType == ResourceType.Board &&
                                   p.ResourceId == board.Id &&
                                   p.SubjectType == SubjectType.User &&
                                   p.SubjectId == context.UserId &&
                                   p.IsRevoked == false &&
                                   (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow), cancellationToken);

                if (boardMember is null && !hasExplicitPermission)
                {
                    return new PermissionDecision(false, "resource_not_found"); // Shielding private board
                }

                // If user has BoardRole, check action
                if (boardMember is not null)
                {
                    if (context.Action == PermissionAction.UpdateItem && boardMember.Role == BoardRole.Observer)
                    {
                        return new PermissionDecision(false, "missing_permission");
                    }
                    return new PermissionDecision(true, null, MapBoardRole(boardMember.Role));
                }

                return new PermissionDecision(true, null, PermissionLevel.Viewer);
            }

            // Workspace board
            if (board.Visibility == BoardVisibility.Workspace)
            {
                // Guest in workspace cannot view private/workspace boards unless explicit member
                if (workspaceMember.Role == WorkspaceRole.Guest)
                {
                    var boardMember = await _context.BoardMembers
                        .FirstOrDefaultAsync(m => m.BoardId == board.Id && m.UserId == context.UserId, cancellationToken);

                    if (boardMember is null)
                    {
                        return new PermissionDecision(false, "resource_not_found");
                    }
                }

                // Check board membership roles
                var boardMemberCheck = await _context.BoardMembers
                    .FirstOrDefaultAsync(m => m.BoardId == board.Id && m.UserId == context.UserId, cancellationToken);

                if (boardMemberCheck is not null)
                {
                    if (context.Action == PermissionAction.UpdateItem && boardMemberCheck.Role == BoardRole.Observer)
                    {
                        return new PermissionDecision(false, "missing_permission");
                    }
                    return new PermissionDecision(true, null, MapBoardRole(boardMemberCheck.Role));
                }

                return new PermissionDecision(true, null, PermissionLevel.Viewer);
            }
        }

        return new PermissionDecision(true, null, PermissionLevel.Viewer);
    }

    private static PermissionLevel MapBoardRole(BoardRole role)
    {
        return role switch
        {
            BoardRole.Observer => PermissionLevel.Viewer,
            BoardRole.Member => PermissionLevel.Editor,
            BoardRole.Admin => PermissionLevel.Owner,
            _ => PermissionLevel.None
        };
    }

    public async Task<bool> AuthorizeAsync(
        Guid userId,
        Guid workspaceId,
        ResourceType resourceType,
        Guid resourceId,
        PermissionAction action,
        CancellationToken cancellationToken = default)
    {
        var decision = await EvaluateAsync(new PermissionContext(userId, workspaceId, resourceType, resourceId, action), cancellationToken);
        return decision.IsAllowed;
    }

    public async Task<bool> AuthorizeWorkspaceAsync(
        Guid userId,
        Guid workspaceId,
        PermissionAction action,
        CancellationToken cancellationToken = default)
    {
        var decision = await EvaluateAsync(new PermissionContext(userId, workspaceId, ResourceType.Workspace, null, action), cancellationToken);
        return decision.IsAllowed;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid workspaceId,
        ResourceType resourceType,
        Guid? resourceId,
        PermissionAction action,
        CancellationToken cancellationToken = default)
    {
        var decision = await EvaluateAsync(new PermissionContext(userId, workspaceId, resourceType, resourceId, action), cancellationToken);
        return decision.IsAllowed;
    }
}
