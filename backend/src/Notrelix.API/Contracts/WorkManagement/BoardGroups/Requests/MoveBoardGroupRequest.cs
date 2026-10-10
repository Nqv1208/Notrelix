namespace Notrelix.API.Contracts.WorkManagement.BoardGroups.Requests;

/// <summary>Neighbor-relative placement of one group; both sibling ids absent appends (SPEC WMREQ132).</summary>
public record MoveBoardGroupRequest(long ExpectedVersion, Guid? PreviousSiblingId, Guid? NextSiblingId);
