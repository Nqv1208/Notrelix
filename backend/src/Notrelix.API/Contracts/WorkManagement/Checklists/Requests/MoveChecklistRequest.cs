namespace Notrelix.API.Contracts.WorkManagement.Checklists.Requests;

public record MoveChecklistRequest(Guid? PreviousSiblingId, Guid? NextSiblingId);
