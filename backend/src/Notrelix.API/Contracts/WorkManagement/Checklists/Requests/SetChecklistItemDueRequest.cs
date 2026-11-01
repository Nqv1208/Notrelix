namespace Notrelix.API.Contracts.WorkManagement.Checklists.Requests;

/// <summary>Sets the due time of a checklist item; a null time clears it.</summary>
public record SetChecklistItemDueRequest(long ExpectedVersion, DateTimeOffset? DueAt);
