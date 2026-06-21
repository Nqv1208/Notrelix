namespace Notrelix.API.Contracts.WorkManagement.Checklists.Requests;

public record UpdateChecklistItemRequest(string? Title, bool? IsChecked, DateTime? DueDate, Guid? AssigneeId);
