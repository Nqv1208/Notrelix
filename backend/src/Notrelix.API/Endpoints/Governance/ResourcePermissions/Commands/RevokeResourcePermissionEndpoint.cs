using Notrelix.API.Extensions;
using Notrelix.Application.Features.Governance.ResourcePermissions.Commands.RevokeResourcePermission;

namespace Notrelix.API.Endpoints.Governance.ResourcePermissions.Commands;

public static class RevokeResourcePermissionEndpoint
{
    public static IEndpointRouteBuilder MapRevokeResourcePermission(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/{permissionId:guid}", HandleAsync)
            .WithName("Governance.ResourcePermissions.Revoke")
            .WithTags("Governance.ResourcePermissions")
            .WithSummary("Revoke a permission from a resource");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        string resourceType,
        Guid resourceId,
        Guid permissionId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RevokeResourcePermissionCommand(Enum.Parse<ResourceType>(resourceType, ignoreCase: true), resourceId, permissionId),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
