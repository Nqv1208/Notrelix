namespace Notrelix.API.Contracts.WorkManagement.Checklists.Requests;

public record MoveChecklistItemRequest(Guid? PreviousSiblingId, Guid? NextSiblingId);
