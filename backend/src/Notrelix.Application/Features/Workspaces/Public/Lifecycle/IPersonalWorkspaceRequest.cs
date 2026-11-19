namespace Notrelix.Application.Features.Workspaces.Public.Lifecycle;

/// <summary>
/// Marker for a write that only records one person's own state about a workspace they can read (inbox entry
/// state, chat read position and notification level, their pins and recents) and changes nothing the workspace
/// holds or anyone else sees. Governance lets these through in an archived workspace; every other write is refused.
/// </summary>
public interface IPersonalWorkspaceRequest;
