namespace Notrelix.API.Contracts.WorkManagement.BoardFields.Requests;

public record CreateBoardFieldRequest(string Name, string Type, string? SettingsJson, double Position);
