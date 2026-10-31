namespace Notrelix.API.Contracts.WorkManagement.BoardPreferences.Requests;

/// <summary>A member's personal state of one view: the opaque criteria and columns JSON (an empty string clears the criteria).</summary>
public record CreateOrUpdateBoardPreferenceRequest(
    string? Criteria = null,
    string? Columns = null);
