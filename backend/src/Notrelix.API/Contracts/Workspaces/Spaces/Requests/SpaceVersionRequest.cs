namespace Notrelix.API.Contracts.Workspaces.Spaces.Requests;

/// <param name="ExpectedVersion">The Space <c>version</c> as last read (SpaceTreeNode.version); a version that moved answers 412 and nothing changes.</param>
public sealed record SpaceVersionRequest(long ExpectedVersion);
