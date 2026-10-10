namespace Notrelix.API.Contracts.WorkManagement.Checklists.Requests;

public record CreateChecklistItemRequest(string Title, Guid? PreviousSiblingId = null, Guid? NextSiblingId = null);
