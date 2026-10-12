using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.BoardAccess;

namespace Notrelix.Infrastructure.CrossContext.Governance.WorkManagement;

/// <summary>
/// Work Management-owned implementation of the published Board access facts
/// (version and members of an active Board) consumed by the Governance access
/// summary. Reads only Work persistence through the Work port; Governance
/// never touches it.
/// </summary>
public sealed class BoardAccessSubjectFactsAdapter : IBoardAccessSubjectFacts
{
    private readonly IWorkManagementDbContext _context;

    public BoardAccessSubjectFactsAdapter(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<BoardAccessSubject?> FindAsync(Guid workspaceId, Guid boardId, CancellationToken cancellationToken)
    {
        var board = await _context.Boards
            .AsNoTracking()
            .Where(b => b.Id == boardId && b.WorkspaceId == workspaceId && !b.IsArchived && b.DeletedAt == null)
            .Select(b => new { b.Id, b.Version })
            .FirstOrDefaultAsync(cancellationToken);
        if (board is null)
        {
            return null;
        }

        var members = await _context.BoardMembers
            .AsNoTracking()
            .Where(m => m.BoardId == boardId)
            .Select(m => m.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return new BoardAccessSubject(board.Id, board.Version, members);
    }
}
