using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Infrastructure.Data.WorkManagement;

public sealed class PostgresBoardItemKeyAliases(ApplicationDbContext context) : IBoardItemKeyAliases
{
    public void Keep(BoardItemKeyAlias alias) => context.BoardItemKeyAliases.Add(alias);
}
