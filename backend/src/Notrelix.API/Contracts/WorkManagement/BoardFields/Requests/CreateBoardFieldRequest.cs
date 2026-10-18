namespace Notrelix.API.Contracts.WorkManagement.BoardFields.Requests;

/// <summary>Neighbor-relative placement; both sibling ids absent appends (SPEC WMREQ131).</summary>
public record CreateBoardFieldRequest(string Name, string Type, string? SettingsJson, Guid? PreviousSiblingId = null, Guid? NextSiblingId = null);
