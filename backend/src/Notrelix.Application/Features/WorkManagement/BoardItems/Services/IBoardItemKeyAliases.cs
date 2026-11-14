namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>Keeps the key an Item held on a board it leaves, so the old key still resolves to the Item.</summary>
public interface IBoardItemKeyAliases
{
    /// <summary>Stages the alias in the current unit of work.</summary>
    void Keep(BoardItemKeyAlias alias);

    /// <summary>The Item that held <paramref name="itemKey"/> on <paramref name="boardId"/> before it moved away, or null.</summary>
    Task<Guid?> FindItemAsync(Guid boardId, string itemKey, CancellationToken cancellationToken);
}
