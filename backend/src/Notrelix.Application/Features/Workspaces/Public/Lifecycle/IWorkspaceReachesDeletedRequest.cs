namespace Notrelix.Application.Features.Workspaces.Public.Lifecycle;

/// <summary>
/// Workspaces-published marker for the one request that must reach a deleted workspace: restore. Every other
/// request on a deleted workspace is NotFound to everyone, the Owner included, so deletion cuts access at once
/// (owner decision 2026-11-01). The request still has to pass the Owner-only RestoreWorkspace permission.
/// </summary>
public interface IWorkspaceReachesDeletedRequest;
