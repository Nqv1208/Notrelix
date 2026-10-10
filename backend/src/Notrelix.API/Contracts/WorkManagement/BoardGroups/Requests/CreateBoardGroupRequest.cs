namespace Notrelix.API.Contracts.WorkManagement.BoardGroups.Requests;

/// <summary>Neighbor-relative placement; both sibling ids absent appends (SPEC WMREQ131).</summary>
public record CreateBoardGroupRequest(string Title, string? Color = null, Guid? PreviousSiblingId = null, Guid? NextSiblingId = null);
