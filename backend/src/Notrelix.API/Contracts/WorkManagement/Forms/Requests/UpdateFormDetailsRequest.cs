namespace Notrelix.API.Contracts.WorkManagement.Forms.Requests;

public record UpdateFormDetailsRequest(string Title, long ExpectedVersion, string? Description = null);
