using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Application.Common.Security;

public class WorkspacePermissionService : IWorkspacePermissionService
{
    private readonly IApplicationDbContext _context;

    public WorkspacePermissionService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanViewWorkspaceAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty || userId == Guid.Empty) return false;

        return await _context.WorkspaceMembers
            .AnyAsync(member => member.WorkspaceId == workspaceId && member.UserId == userId, cancellationToken);
    }

    public async Task<bool> CanEditWorkspaceAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        var role = await GetWorkspaceRoleAsync(workspaceId, userId, cancellationToken);
        return role is WorkspaceRole.Owner or WorkspaceRole.Admin or WorkspaceRole.Member;
    }

    public async Task<bool> CanManageWorkspaceAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        var role = await GetWorkspaceRoleAsync(workspaceId, userId, cancellationToken);
        return role is WorkspaceRole.Owner or WorkspaceRole.Admin;
    }

    public async Task<bool> CanEditBoardAsync(Guid boardId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (boardId == Guid.Empty || userId == Guid.Empty) return false;

        var board = await _context.Boards
            .AsNoTracking()
            .Where(item => item.Id == boardId && !item.IsArchived)
            .Select(item => new { item.Id, item.WorkspaceId })
            .FirstOrDefaultAsync(cancellationToken);

        if (board is null) return false;

        var workspaceRole = await GetWorkspaceRoleAsync(board.WorkspaceId, userId, cancellationToken);
        if (workspaceRole is WorkspaceRole.Owner or WorkspaceRole.Admin or WorkspaceRole.Member)
        {
            return true;
        }

        var boardRole = await GetBoardRoleAsync(board.Id, userId, cancellationToken);
        return boardRole is BoardRole.Admin or BoardRole.Member;
    }

    public async Task<bool> CanManageBoardAsync(Guid boardId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (boardId == Guid.Empty || userId == Guid.Empty) return false;

        var board = await _context.Boards
            .AsNoTracking()
            .Where(item => item.Id == boardId && !item.IsArchived)
            .Select(item => new { item.Id, item.WorkspaceId })
            .FirstOrDefaultAsync(cancellationToken);

        if (board is null) return false;

        var workspaceRole = await GetWorkspaceRoleAsync(board.WorkspaceId, userId, cancellationToken);
        if (workspaceRole is WorkspaceRole.Owner or WorkspaceRole.Admin)
        {
            return true;
        }

        var boardRole = await GetBoardRoleAsync(board.Id, userId, cancellationToken);
        return boardRole is BoardRole.Admin;
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

    private async Task<WorkspaceRole?> GetWorkspaceRoleAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        if (workspaceId == Guid.Empty || userId == Guid.Empty) return null;

        return await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(member => member.WorkspaceId == workspaceId && member.UserId == userId)
            .Select(member => (WorkspaceRole?)member.Role)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<BoardRole?> GetBoardRoleAsync(Guid boardId, Guid userId, CancellationToken cancellationToken)
    {
        return await _context.BoardMembers
            .AsNoTracking()
            .Where(member => member.BoardId == boardId && member.UserId == userId)
            .Select(member => (BoardRole?)member.Role)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
