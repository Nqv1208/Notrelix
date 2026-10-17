namespace Notrelix.Application.Features.WorkManagement.Public.Authorization;

/// <summary>
/// Work Management-published authorization fact: the Board a Board-scoped
/// resource lives in. Governance decides every Board child resource on its
/// parent Board (GOV-033) and reads that relation through this contract,
/// never through Work persistence. Deleted children are resolved too (restore
/// flows); the Board's own lifecycle is decided by the access facts.
/// </summary>
public interface IBoardChildAuthorizationFacts
{
    /// <param name="resourceKind">The canonical resource kind value, e.g. <c>work-management.board-item</c>.</param>
    Task<BoardChildScope> ResolveAsync(string resourceKind, Guid resourceId, CancellationToken cancellationToken);
}

/// <param name="IsBoardChild">The kind is a Board child kind (Item, Group, Field, View, Label, Checklist, Checklist item).</param>
/// <param name="BoardId">The parent Board; null when a Board child does not exist.</param>
public sealed record BoardChildScope(bool IsBoardChild, Guid? BoardId)
{
    public static readonly BoardChildScope NotBoardChild = new(false, null);
}
