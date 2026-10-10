namespace Notrelix.API.Contracts.WorkManagement.Checklists.Requests;

public record UpdateChecklistRequest(long ExpectedVersion, string? Title);
