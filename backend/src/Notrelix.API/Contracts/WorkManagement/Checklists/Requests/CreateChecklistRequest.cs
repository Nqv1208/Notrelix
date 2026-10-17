namespace Notrelix.API.Contracts.WorkManagement.Checklists.Requests;

public record CreateChecklistRequest(string Title = "Checklist", Guid? PreviousSiblingId = null, Guid? NextSiblingId = null);
