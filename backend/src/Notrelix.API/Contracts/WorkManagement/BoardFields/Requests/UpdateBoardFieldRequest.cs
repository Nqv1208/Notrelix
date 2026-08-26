namespace Notrelix.API.Contracts.WorkManagement.BoardFields.Requests;

public record UpdateBoardFieldRequest(string Name, string Type, string? SettingsJson, long? ExpectedVersion = null);
