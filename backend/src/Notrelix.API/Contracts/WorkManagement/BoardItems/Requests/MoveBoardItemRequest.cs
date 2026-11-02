namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

/// <summary>
/// Neighbor-relative placement in the target group (SPEC WMREQ131): both sibling ids
/// absent appends; the server generates the persisted position. A card dropped in a Kanban lane may name the
/// lane's field and option (both or neither): the option is written with the placement in one change.
/// </summary>
public record MoveBoardItemRequest(Guid GroupId, long ExpectedVersion, Guid? PreviousSiblingId, Guid? NextSiblingId, Guid? LaneFieldId = null, Guid? LaneOptionId = null);
