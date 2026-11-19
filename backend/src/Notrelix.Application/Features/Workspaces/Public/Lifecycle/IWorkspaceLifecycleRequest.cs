namespace Notrelix.Application.Features.Workspaces.Public.Lifecycle;

/// <summary>
/// Workspaces-published marker for the requests that must still work in an archived workspace: archive (an
/// idempotent no-op), unarchive, delete, restore and leave. Every other write in an archived workspace is refused
/// with <c>workspace.archived</c> (the workspace is read-only for every member until unarchived).
/// </summary>
public interface IWorkspaceLifecycleRequest;
