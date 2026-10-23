namespace Notrelix.API.Contracts.Workspaces.Members.Requests;

/// <param name="JobTitle">The Workspace job title (at most 100 characters, no control characters); null or blank clears it.</param>
/// <param name="ExpectedVersion">The <c>version</c> of the member from the last read of the member list.</param>
public sealed record UpdateMemberJobTitleRequest(string? JobTitle, long ExpectedVersion);
