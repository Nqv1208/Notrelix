namespace Notrelix.API.Contracts.WorkManagement.Checklists.Requests;

/// <summary>Copy the items of another checklist of the same board into a new checklist on this item.</summary>
public record CopyChecklistRequest(Guid SourceChecklistId, string? Title = null);
