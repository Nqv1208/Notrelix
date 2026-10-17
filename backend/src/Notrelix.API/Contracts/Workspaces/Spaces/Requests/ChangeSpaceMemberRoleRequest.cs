namespace Notrelix.API.Contracts.Workspaces.Spaces.Requests;

/// <param name="Role"><c>Admin</c>, <c>Member</c> or <c>Viewer</c>.</param>
/// <param name="ExpectedVersion">The Space version the client last read.</param>
public sealed record ChangeSpaceMemberRoleRequest(string Role, long ExpectedVersion);
