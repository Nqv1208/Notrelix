namespace Notrelix.API.Contracts.Workspaces.Spaces.Requests;

/// <param name="SubjectType"><c>User</c> (an active member of the workspace) or <c>Team</c> (an active team of the workspace).</param>
/// <param name="Role"><c>Admin</c>, <c>Member</c> or <c>Viewer</c>.</param>
/// <param name="ExpectedVersion">The Space version the client last read.</param>
public sealed record AddSpaceMemberRequest(string SubjectType, Guid SubjectId, string Role, long ExpectedVersion);
