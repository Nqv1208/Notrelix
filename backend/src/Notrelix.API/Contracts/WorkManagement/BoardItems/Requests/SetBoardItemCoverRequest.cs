namespace Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;

/// <summary>A colour token or an image attachment id of the item (not both), shown as <c>strip</c> or <c>full</c>; all null removes the cover.</summary>
public record SetBoardItemCoverRequest(string? Color, Guid? AttachmentId, string? Style, long ExpectedVersion);
