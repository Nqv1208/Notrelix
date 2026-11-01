namespace Notrelix.Application.Features.Governance.Roles.Queries.ExplainAccess;

internal sealed record ExplainResourceProbe(PermissionAction Action, ResourceRef Target)
    : IAuthenticatedRequest, IResourceScopedRequest, IReadRequest, IRequirePermission
{
    public ResourceRef Resource => Target;
    ResourceRef? IRequirePermission.Resource => Target;
}
