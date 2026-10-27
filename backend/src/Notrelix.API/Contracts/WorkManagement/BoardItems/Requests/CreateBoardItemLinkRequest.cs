namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

/// <summary>"This item <paramref name="Relation"/> the other item": <c>blocked-by</c>, <c>blocks</c> or <c>related</c>.</summary>
public record CreateBoardItemLinkRequest(Guid ItemId, string Relation);
