using Notrelix.API.Contracts.Governance.ResourcePermissions.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Governance.ResourcePermissions.Commands.GrantResourcePermission;

namespace Notrelix.API.Endpoints.Governance.ResourcePermissions.Commands;

public static class GrantResourcePermissionEndpoint
{
    public static IEndpointRouteBuilder MapGrantResourcePermission(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/", HandleAsync)
            .WithName("Governance.ResourcePermissions.Grant")
            .WithTags("Governance.ResourcePermissions")
            .WithSummary("Grant a permission to a resource");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        string resourceType,
        Guid resourceId,
        GrantPermissionRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GrantResourcePermissionCommand(Enum.Parse<ResourceType>(resourceType, ignoreCase: true), resourceId, body.SubjectType, body.SubjectId, body.Level, body.ExpiresAt),
            cancellationToken);
        return result.ToCreatedResult();
    }
}
