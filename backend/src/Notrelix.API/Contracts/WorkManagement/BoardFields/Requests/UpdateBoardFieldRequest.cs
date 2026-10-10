namespace Notrelix.API.Contracts.WorkManagement.BoardFields.Requests;

/// <summary>
/// Ordinary field PATCH (SPEC WMREQ030): name and settings only; omitted values are
/// unchanged and the field type is not patchable.
/// </summary>
public record UpdateBoardFieldRequest(string? Name, string? SettingsJson, long ExpectedVersion);
