namespace Notrelix.API.Contracts.WorkManagement.Forms.Requests;

public record CreateFormRequest(string Title, string? Description = null);
