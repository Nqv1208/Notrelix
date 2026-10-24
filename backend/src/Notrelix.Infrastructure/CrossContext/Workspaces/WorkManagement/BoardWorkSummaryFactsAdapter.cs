using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.BoardWork;
using Notrelix.Domain.WorkManagement.Approvals;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Views;

namespace Notrelix.Infrastructure.CrossContext.Workspaces.WorkManagement;

/// <summary>
/// Work Management-owned implementation of the published Board work figures consumed by the Workspaces
/// Space overview. Reads only Work persistence through the Work port; the caller supplies the readable
/// Board ids and this adapter never widens them.
/// </summary>
public sealed class BoardWorkSummaryFactsAdapter : IBoardWorkSummaryFacts
{
    private const string ItemKind = "work-management.board-item";
    private const string BoardKind = "work-management.board";
    private const int TopAssigneesPerBoard = 3;

    private readonly IWorkManagementDbContext _context;

    public BoardWorkSummaryFactsAdapter(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BoardWorkSummary>> SummarizeAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> spaceIds,
        IReadOnlyCollection<Guid> readableBoardIds,
        bool includePrivate,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (spaceIds.Count == 0 || readableBoardIds.Count == 0)
            return [];

        var boards = await PlacedBoards(workspaceId, spaceIds, readableBoardIds, includePrivate)
            .Select(b => new { b.Id, SpaceId = b.SpaceId!.Value, b.Title, b.BoardFamily, b.Visibility, At = b.UpdatedAt ?? b.CreatedAt })
            .ToListAsync(cancellationToken);
        if (boards.Count == 0)
            return [];

        var ids = boards.Select(b => b.Id).ToList();
        var liveItems = _context.BoardItems
            .AsNoTracking()
            .Where(i => ids.Contains(i.BoardId) && i.DeletedAt == null && !i.IsArchived);

        var itemFigures = (await liveItems
            .GroupBy(i => i.BoardId)
            .Select(g => new
            {
                BoardId = g.Key,
                Open = g.Count(i => i.CompletedAt == null),
                Done = g.Count(i => i.ParentItemId == null && i.CompletedAt != null),
                Total = g.Count(i => i.ParentItemId == null),
                Overdue = g.Count(i => i.CompletedAt == null && i.DueAt != null && i.DueAt < now),
                Last = g.Max(i => i.UpdatedAt ?? i.CreatedAt),
            })
            .ToListAsync(cancellationToken)).ToDictionary(f => f.BoardId);

        var unclaimed = (await liveItems
            .Where(i => i.ParentItemId == null && i.CompletedAt == null
                && !_context.BoardItemMembers.Any(m => m.ItemId == i.Id))
            .GroupBy(i => i.BoardId)
            .Select(g => new { BoardId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)).ToDictionary(x => x.BoardId, x => x.Count);

        var onBoard = (await _context.ApprovalRequests
            .AsNoTracking()
            .Where(a => a.DeletedAt == null && a.Status == ApprovalStatus.Pending
                && a.Target.Kind == Notrelix.Domain.SharedKernel.ResourceKind.Create(BoardKind)
                && ids.Contains(a.Target.ResourceId))
            .GroupBy(a => a.Target.ResourceId)
            .Select(g => new { BoardId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)).ToDictionary(x => x.BoardId, x => x.Count);

        var onItems = (await (
            from a in _context.ApprovalRequests.AsNoTracking()
            join i in liveItems on a.Target.ResourceId equals i.Id
            where a.DeletedAt == null && a.Status == ApprovalStatus.Pending
                && a.Target.Kind == Notrelix.Domain.SharedKernel.ResourceKind.Create(ItemKind)
                && i.CompletedAt == null
            group a by i.BoardId into g
            select new { BoardId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)).ToDictionary(x => x.BoardId, x => x.Count);

        var members = (await _context.BoardMembers
            .AsNoTracking()
            .Where(m => ids.Contains(m.BoardId))
            .GroupBy(m => m.BoardId)
            .Select(g => new { BoardId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)).ToDictionary(x => x.BoardId, x => x.Count);

        var views = await _context.BoardViews
            .AsNoTracking()
            .Where(v => ids.Contains(v.BoardId) && !v.IsArchived)
            .OrderByDescending(v => v.IsDefault)
            .ThenBy(v => v.CreatedAt)
            .Select(v => new { v.BoardId, v.Type })
            .ToListAsync(cancellationToken);
        var firstView = views.GroupBy(v => v.BoardId).ToDictionary(g => g.Key, g => g.First().Type);

        var assignees = (await (
            from m in _context.BoardItemMembers.AsNoTracking()
            join i in liveItems on m.ItemId equals i.Id
            where i.CompletedAt == null
            group i by new { m.BoardId, m.UserId } into g
            select new
            {
                g.Key.BoardId,
                g.Key.UserId,
                Open = g.Select(i => i.Id).Distinct().Count(),
                Overdue = g.Where(i => i.DueAt != null && i.DueAt < now).Select(i => i.Id).Distinct().Count(),
            })
            .ToListAsync(cancellationToken))
            .GroupBy(x => x.BoardId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<PersonOpenWork>)g
                    .OrderByDescending(x => x.Open).ThenByDescending(x => x.Overdue).ThenBy(x => x.UserId)
                    .Take(TopAssigneesPerBoard)
                    .Select(x => new PersonOpenWork(x.UserId, x.Open, x.Overdue))
                    .ToList());

        return boards.Select(b =>
        {
            itemFigures.TryGetValue(b.Id, out var f);
            var kind = b.BoardFamily == BoardFamily.Intake
                ? BoardWorkKinds.Inbox
                : firstView.TryGetValue(b.Id, out var type) && type == ViewType.Kanban ? BoardWorkKinds.Kanban : BoardWorkKinds.Table;
            var last = f is not null && f.Last > b.At ? f.Last : b.At;
            return new BoardWorkSummary(
                b.Id,
                b.SpaceId,
                b.Title,
                kind,
                b.Visibility == BoardVisibility.Private,
                members.GetValueOrDefault(b.Id),
                f?.Open ?? 0,
                f?.Done ?? 0,
                f?.Total ?? 0,
                f?.Overdue ?? 0,
                onBoard.GetValueOrDefault(b.Id) + onItems.GetValueOrDefault(b.Id),
                unclaimed.GetValueOrDefault(b.Id),
                last,
                assignees.GetValueOrDefault(b.Id) ?? []);
        }).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountBoardsBySpaceAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> spaceIds,
        IReadOnlyCollection<Guid> readableBoardIds,
        bool includePrivate,
        CancellationToken cancellationToken)
    {
        if (spaceIds.Count == 0 || readableBoardIds.Count == 0)
            return new Dictionary<Guid, int>();

        return (await PlacedBoards(workspaceId, spaceIds, readableBoardIds, includePrivate)
            .GroupBy(b => b.SpaceId!.Value)
            .Select(g => new { SpaceId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)).ToDictionary(x => x.SpaceId, x => x.Count);
    }

    public async Task<IReadOnlyList<PersonOpenWork>> OpenWorkByPersonAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> boardIds,
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken)
    {
        if (boardIds.Count == 0)
            return [];

        var rows = await (
            from m in _context.BoardItemMembers.AsNoTracking()
            join i in _context.BoardItems.AsNoTracking() on m.ItemId equals i.Id
            where m.WorkspaceId == workspaceId
                && boardIds.Contains(i.BoardId)
                && i.DeletedAt == null && !i.IsArchived && i.CompletedAt == null
            group i by m.UserId into g
            select new
            {
                UserId = g.Key,
                Open = g.Select(i => i.Id).Distinct().Count(),
                Overdue = g.Where(i => i.DueAt != null && i.DueAt < now).Select(i => i.Id).Distinct().Count(),
            })
            .OrderByDescending(x => x.Open).ThenByDescending(x => x.Overdue).ThenBy(x => x.UserId)
            .Take(take)
            .ToListAsync(cancellationToken);
        return rows.Select(x => new PersonOpenWork(x.UserId, x.Open, x.Overdue)).ToList();
    }

    private IQueryable<Board> PlacedBoards(
        Guid workspaceId,
        IReadOnlyCollection<Guid> spaceIds,
        IReadOnlyCollection<Guid> readableBoardIds,
        bool includePrivate) =>
        _context.Boards
            .AsNoTracking()
            .Where(b => b.WorkspaceId == workspaceId
                && b.DeletedAt == null
                && !b.IsArchived
                && b.SpaceId != null
                && spaceIds.Contains(b.SpaceId.Value)
                && readableBoardIds.Contains(b.Id)
                && (includePrivate || b.Visibility != BoardVisibility.Private));
}
