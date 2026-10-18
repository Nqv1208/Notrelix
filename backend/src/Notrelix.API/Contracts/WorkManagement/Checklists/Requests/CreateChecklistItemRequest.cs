namespace Notrelix.API.Contracts.WorkManagement.Checklists.Requests;

public record CreateChecklistItemRequest(string Title, long ExpectedVersion, Guid? PreviousSiblingId = null, Guid? NextSiblingId = null);
