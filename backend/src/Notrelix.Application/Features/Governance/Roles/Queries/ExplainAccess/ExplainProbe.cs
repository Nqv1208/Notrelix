namespace Notrelix.Application.Features.Governance.Roles.Queries.ExplainAccess;

internal sealed record ExplainProbe(Guid WorkspaceId, PermissionAction Action)
    : IAuthenticatedRequest, IWorkspaceRequest, IReadRequest, IRequirePermission
{
    // A workspace-level action is asked of the workspace itself, as the real requests do.
    public ResourceRef? Resource => ResourceRef.Create(
        Notrelix.Domain.SharedKernel.ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}
