namespace Notrelix.API.Contracts.WorkManagement.Checklists.Requests;

public record MoveChecklistItemRequest(long ExpectedVersion, Guid? PreviousSiblingId, Guid? NextSiblingId);
