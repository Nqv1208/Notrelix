namespace Notrelix.API.Contracts.WorkManagement.Checklists.Requests;

/// <summary>Desired-state PATCH: an omitted field is unchanged (WM-V3-APP-CHK-001).</summary>
public record UpdateChecklistItemRequest(string? Title, bool? IsChecked);
