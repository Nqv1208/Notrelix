using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Infrastructure.Data.WorkManagement;

public sealed class PostgresBoardItemKeyAliases(ApplicationDbContext context) : IBoardItemKeyAliases
{
    public void Keep(BoardItemKeyAlias alias) => context.BoardItemKeyAliases.Add(alias);

    public async Task<Guid?> FindItemAsync(Guid boardId, string itemKey, CancellationToken cancellationToken) =>
        await context.BoardItemKeyAliases.AsNoTracking()
            .Where(a => a.BoardId == boardId && a.ItemKey == itemKey)
            .Select(a => (Guid?)a.ItemId)
            .FirstOrDefaultAsync(cancellationToken);
}
