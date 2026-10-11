using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.Common;

/// <summary>
/// Shared Board list read: active Boards of one Workspace restricted to the ids
/// the caller may see, with member and group counts aggregated in the same
/// bounded set of queries (no per-Board round trip).
/// </summary>
internal static class BoardListProjection
{
    public static async Task<List<BoardDto>> LoadAsync(
        IWorkManagementDbContext context,
        Guid workspaceId,
        IReadOnlyList<Guid> visibleBoardIds,
        CancellationToken ct)
    {
        if (visibleBoardIds.Count == 0)
        {
            return [];
        }

        var boards = await context.Boards.AsNoTracking()
            .Where(b => b.WorkspaceId == workspaceId && !b.IsArchived && visibleBoardIds.Contains(b.Id))
            .ToListAsync(ct);

        var boardIds = boards.Select(b => b.Id).ToList();

        var memberCounts = await context.BoardMembers.AsNoTracking()
            .Where(m => boardIds.Contains(m.BoardId))
            .GroupBy(m => m.BoardId)
            .Select(g => new { BoardId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BoardId, x => x.Count, ct);

        var groupCounts = await context.BoardGroups.AsNoTracking()
            .Where(l => boardIds.Contains(l.BoardId) && l.DeletedAt == null)
            .GroupBy(l => l.BoardId)
            .Select(g => new { BoardId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BoardId, x => x.Count, ct);

        return boards.Select(b => new BoardDto(
            b.Id, b.WorkspaceId, b.Title, b.Description,
            b.Background, b.Visibility.ToString(), b.IsArchived,
            memberCounts.GetValueOrDefault(b.Id),
            groupCounts.GetValueOrDefault(b.Id),
            b.CreatedAt.DateTime,
            b.Version
        )).ToList();
    }
}
