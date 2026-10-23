using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.GettingStarted;

namespace Notrelix.Infrastructure.CrossContext.Workspaces.WorkManagement;

/// <summary>Work Management-owned implementation of the published first-Item fact consumed by the Workspaces getting-started card.</summary>
public sealed class FirstWorkItemFactsAdapter : IFirstWorkItemFacts
{
    private readonly IWorkManagementDbContext _context;

    public FirstWorkItemFactsAdapter(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<FirstWorkItemBoard?> FindEarliestItemBoardAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> readableBoardIds,
        CancellationToken cancellationToken)
    {
        if (readableBoardIds.Count == 0)
            return null;

        return await (
            from i in _context.BoardItems.AsNoTracking()
            join b in _context.Boards.AsNoTracking() on i.BoardId equals b.Id
            where i.WorkspaceId == workspaceId && i.DeletedAt == null && !i.IsArchived
                && b.DeletedAt == null && !b.IsArchived && readableBoardIds.Contains(b.Id)
            orderby i.CreatedAt, i.Id
            select new FirstWorkItemBoard(b.Id, b.Title))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FirstWorkItemBoard?> FindNewestBoardAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> readableBoardIds,
        CancellationToken cancellationToken)
    {
        if (readableBoardIds.Count == 0)
            return null;

        return await _context.Boards
            .AsNoTracking()
            .Where(b => b.WorkspaceId == workspaceId && b.DeletedAt == null && !b.IsArchived && readableBoardIds.Contains(b.Id))
            .OrderByDescending(b => b.CreatedAt)
            .ThenByDescending(b => b.Id)
            .Select(b => new FirstWorkItemBoard(b.Id, b.Title))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
