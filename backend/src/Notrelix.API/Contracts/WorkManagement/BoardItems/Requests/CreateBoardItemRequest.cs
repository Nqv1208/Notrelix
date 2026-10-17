namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

/// <summary>
/// Neighbor-relative placement (SPEC WMREQ131): both sibling ids absent appends to the
/// group; the server generates the persisted position.
/// </summary>
public record CreateBoardItemRequest(Guid GroupId, string Title, Guid? PreviousSiblingId, Guid? NextSiblingId);
