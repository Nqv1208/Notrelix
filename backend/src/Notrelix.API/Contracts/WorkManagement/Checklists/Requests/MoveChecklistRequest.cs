namespace Notrelix.API.Contracts.WorkManagement.Checklists.Requests;

public record MoveChecklistRequest(long ExpectedVersion, Guid? PreviousSiblingId, Guid? NextSiblingId);
