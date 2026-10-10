namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

/// <summary>
/// Neighbor-relative placement in the target group (SPEC WMREQ131): both sibling ids
/// absent appends; the server generates the persisted position.
/// </summary>
public record MoveBoardItemRequest(Guid GroupId, long ExpectedVersion, Guid? PreviousSiblingId, Guid? NextSiblingId);
