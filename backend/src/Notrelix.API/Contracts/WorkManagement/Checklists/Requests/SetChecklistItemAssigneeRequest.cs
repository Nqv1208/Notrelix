namespace Notrelix.API.Contracts.WorkManagement.Checklists.Requests;

/// <summary>Gives a checklist item to a person; a null person clears it.</summary>
public record SetChecklistItemAssigneeRequest(long ExpectedVersion, Guid? AssigneeUserId);
