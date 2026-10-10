namespace Notrelix.API.Contracts.WorkManagement.BoardFields.Requests;

/// <summary>Neighbor-relative placement of one field; both sibling ids absent appends (SPEC WMREQ132).</summary>
public record MoveBoardFieldRequest(Guid? PreviousSiblingId, Guid? NextSiblingId);
