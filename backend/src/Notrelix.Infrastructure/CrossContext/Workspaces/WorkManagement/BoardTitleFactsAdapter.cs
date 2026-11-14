using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.BoardTitles;

namespace Notrelix.Infrastructure.CrossContext.Workspaces.WorkManagement;

/// <summary>Work Management-owned implementation of the published Board title fact consumed by Chat (UC-CHT-H10).</summary>
public sealed class BoardTitleFactsAdapter(IWorkManagementDbContext context) : IBoardTitleFacts
{
    public async Task<IReadOnlyDictionary<Guid, string>> TitlesAsync(
        Guid workspaceId, IReadOnlyCollection<Guid> boardIds, CancellationToken cancellationToken)
    {
        if (boardIds.Count == 0) return new Dictionary<Guid, string>();
        return await context.Boards
            .AsNoTracking()
            .Where(b => b.WorkspaceId == workspaceId && boardIds.Contains(b.Id) && b.DeletedAt == null)
            .ToDictionaryAsync(b => b.Id, b => b.Title, cancellationToken);
    }
}
