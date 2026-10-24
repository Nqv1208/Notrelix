namespace Notrelix.API.Contracts.Workspaces.Workspaces.Requests;

/// <param name="Slug">The address the caller chose (checked with the slug check); when absent it is derived from the name.</param>
/// <param name="Size">The company-size band: <c>1-10</c>, <c>11-50</c>, <c>51-200</c>, <c>201-1000</c> or <c>1000+</c>.</param>
/// <param name="Language">The workspace language, <c>vi</c> or <c>en</c>.</param>
public record CreateWorkspaceRequest(string Name, string? Description, bool IsPersonal, string? Slug = null, string? Size = null, string? Language = null);
