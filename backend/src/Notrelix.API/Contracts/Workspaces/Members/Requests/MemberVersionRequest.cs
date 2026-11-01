namespace Notrelix.API.Contracts.Workspaces.Members.Requests;

/// <param name="ExpectedVersion">The member's <c>version</c> as last read; a version that moved answers 412 and nothing changes.</param>
public sealed record MemberVersionRequest(long ExpectedVersion);
