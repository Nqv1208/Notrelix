using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.SpacePlacement;

namespace Notrelix.Infrastructure.CrossContext.Workspaces.WorkManagement;

/// <summary>
/// Work Management-owned implementation of the published Board placement
/// facts consumed by Workspaces (Space archive/delete, WSP-022). Reads only
/// Work persistence through the Work port; Workspaces never touches it.
/// </summary>
public sealed class BoardSpacePlacementFactsAdapter : IBoardSpacePlacementFacts
{
    private readonly IWorkManagementDbContext _context;

    public BoardSpacePlacementFactsAdapter(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<SpacePlacedBoardCount> CountInSpaceAsync(Guid workspaceId, Guid spaceId, CancellationToken cancellationToken)
    {
        var counts = await _context.Boards
            .AsNoTracking()
            .Where(b => b.WorkspaceId == workspaceId && b.SpaceId == spaceId)
            .GroupBy(_ => 1)
            .Select(g => new { Active = g.Count(b => !b.IsArchived), Existing = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        return counts is null ? new SpacePlacedBoardCount(0, 0) : new SpacePlacedBoardCount(counts.Active, counts.Existing);
    }
}
