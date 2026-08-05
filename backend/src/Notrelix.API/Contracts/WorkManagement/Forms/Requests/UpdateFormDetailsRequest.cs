namespace Notrelix.API.Contracts.WorkManagement.Forms.Requests;

public record UpdateFormDetailsRequest(string Title, string? Description = null);
