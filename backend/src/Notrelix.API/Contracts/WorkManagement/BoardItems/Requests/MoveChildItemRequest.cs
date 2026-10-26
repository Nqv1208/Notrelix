namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

/// <summary>Neighbor-relative placement among the parent's sub-items; both absent appends.</summary>
public record MoveChildItemRequest(long ExpectedVersion, Guid? PreviousSiblingId, Guid? NextSiblingId);
