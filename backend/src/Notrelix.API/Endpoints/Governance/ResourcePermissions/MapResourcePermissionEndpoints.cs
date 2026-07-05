using Notrelix.API.Endpoints.Governance.ResourcePermissions.Commands;
using Notrelix.API.Endpoints.Governance.ResourcePermissions.Queries;

namespace Notrelix.API.Endpoints.Governance.ResourcePermissions;

public static class MapResourcePermissionEndpoints
{
    public static IEndpointRouteBuilder MapResourcePermissionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/resources/{resourceType}/{resourceId:guid}/permissions")
            .WithTags("Governance.ResourcePermissions")
            .WithOpenApi();

        group.MapGetResourcePermissions();
        group.MapGrantResourcePermission();
        group.MapRevokeResourcePermission();

        return app;
    }
}
