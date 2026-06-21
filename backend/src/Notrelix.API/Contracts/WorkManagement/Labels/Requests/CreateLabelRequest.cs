namespace Notrelix.API.Contracts.WorkManagement.Labels.Requests;

public record CreateLabelRequest(string Color, string? Name = null);
