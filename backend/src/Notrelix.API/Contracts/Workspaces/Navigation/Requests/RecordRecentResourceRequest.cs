namespace Notrelix.API.Contracts.Workspaces.Navigation.Requests;

/// <summary>
/// <c>Kind</c> is <c>work-management.board</c> or <c>documents.page</c>.
/// </summary>
public sealed record RecordRecentResourceRequest(string Kind, Guid ResourceId);
