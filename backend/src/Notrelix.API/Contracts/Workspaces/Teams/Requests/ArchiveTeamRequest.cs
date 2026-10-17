namespace Notrelix.API.Contracts.Workspaces.Teams.Requests;

/// <summary>Body of Team archive and unarchive: the Team version the caller read.</summary>
public sealed record ArchiveTeamRequest(long ExpectedVersion);
